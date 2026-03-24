import { Dirent } from 'node:fs';
import { readdir, readFile } from 'node:fs/promises';
import { resolve, dirname, join } from 'path';
import { fileURLToPath } from 'node:url';
import type { DocsFile } from '../types';
import matter from 'gray-matter';

export async function collectDocsRecursive(dir: string, parentPosition: number[] = []): Promise<DocsFile[]> {
  const docs: DocsFile[] = [];

  // Load files
  const pageFiles = await extractFiles(dir);
  const fileDocs = (
    await Promise.all(
      pageFiles.map(async page => {
        const fullPath = join(dir, page.name);
        const raw = await readFile(fullPath, 'utf-8');
        const parsed = matter(raw);
        const position = parsed.data.sidebar_position;

        if (typeof position === 'number') {
          return {
            title: parsed.data.title ?? page.name,
            position: [...parentPosition, position],
            content: parsed.content,
            path: fullPath,
          } as DocsFile;
        }

        return null;
      })
    )
  ).filter((d): d is DocsFile => d !== null);

  const validDocs = fileDocs.filter((d): d is DocsFile => d !== null);
  docs.push(...validDocs);

  // Recurse into subdirs
  const subDirs = await extractSubdirectories(dir);
  for (const subDir of subDirs) {
    const subDirPath = join(dir, subDir.name);
    const subDirPosition = await readCategoryPosition(subDirPath);

    if (subDirPosition !== null) {
      const nested = await collectDocsRecursive(subDirPath, [...parentPosition, subDirPosition]);
      docs.push(...nested);
    }
  }
  return docs;
}

export function orderDocFileArray(docsFiles: DocsFile[]): DocsFile[] {
  return docsFiles.sort((a, b) => {
    for (let i = 0; i < Math.max(a.position.length, b.position.length); i++) {
      const aVal = a.position[i] ?? 0;
      const bVal = b.position[i] ?? 0;
      if (aVal !== bVal) return aVal - bVal;
    }
    return 0;
  });
}

export async function getPositionAndContent(filename: string): Promise<DocsFile | null> {
  const rawContent = await readFile(filename, 'utf-8');
  const parsed = matter(rawContent);

  const position = parsed.data.sidebar_position;

  if (typeof position !== 'number') return null;

  return {
    title: parsed.data.title ?? filename,
    position: [position],
    content: parsed.content,
    path: filename,
  };
}

export async function readCategoryPosition(dir: string): Promise<number | null> {
  try {
    const raw = await readFile(join(dir, '_category_.json'), 'utf-8');
    const parsed = JSON.parse(raw);
    return typeof parsed.position === 'number' ? parsed.position : null;
  } catch {
    return null;
  }
}

export async function extractFiles(docsDir: string): Promise<Dirent[]> {
  const entries = await readdir(docsDir, { withFileTypes: true });

  // This was a fun one to learn for regex to keep this lean
  return entries.filter(file => /\.(md|mdx)$/.test(file.name));
}

export async function extractSubdirectories(docsDir: string): Promise<Dirent[]> {
  const entries = await readdir(docsDir, { withFileTypes: true });

  return entries.filter(entry => entry.isDirectory());
}

export async function getDocsFilesAndSubDirectories(docsDir: string): Promise<string[]> {
  const files = await readdir(docsDir);

  return files;
}

export async function getDocsDirectory(targetName: string, silent: boolean = false): Promise<string> {
  let currentDir = dirname(fileURLToPath(import.meta.url));

  while (true) {
    const entries = await readdir(currentDir);

    if (entries.includes(targetName)) {
      if (!silent) {
        console.log(`🔍 Found the docs dir`);
      }
      return resolve(currentDir, targetName);
    }

    const parentDir = dirname(currentDir);
    if (parentDir === currentDir) {
      throw new Error(`Could not find '${targetName}' in any parent directories.`);
    }

    currentDir = parentDir;
  }
}
