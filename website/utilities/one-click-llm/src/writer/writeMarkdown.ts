import type { DocsFile } from '../types';
import { join, dirname, resolve } from 'path';
import { readFile, writeFile, mkdir } from 'fs/promises';
import { fileURLToPath } from 'node:url';

const BASE_URL = 'https://hasura.io/docs/3.0';

async function expandPartials(content: string, docPath: string, rootDir: string, depth: number = 0): Promise<string> {
  if (depth > 10) {
    console.warn('Maximum recursion depth reached when expanding partials');
    return content;
  }

  // First, find all imports from @site/docs/ or @theme
  const importMatches = Array.from(content.matchAll(/import\s+(\w+)\s+from\s+"(@site\/docs\/|@theme\/)([^"]+)"/g));
  const importMap = new Map();
  const expectedPartials = new Set();

  for (const match of importMatches) {
    const [_, componentName, prefix, filePath] = match;
    if (!componentName || !filePath) continue;

    if (prefix === '@site/docs/') {
      expectedPartials.add(componentName);
      const fullPath = join(rootDir, 'docs', filePath);
      try {
        const importContent = await readFile(fullPath, 'utf-8');
        // Recursively expand partials in the imported content
        const expandedContent = await expandPartials(importContent, fullPath, rootDir, depth + 1);
        importMap.set(componentName, expandedContent);
      } catch (error) {
        console.warn(`Could not find imported file: ${filePath}`);
      }
    }
  }

  // Find all component usages
  const componentUsages = Array.from(content.matchAll(/<(\w+)\s*\/>/g));
  let expandedContent = content;

  // Try to find and read the partial files
  for (const match of componentUsages) {
    const [fullMatch, componentName] = match;
    if (!componentName) continue;

    // First check if we have this component in our importMap
    if (importMap.has(componentName)) {
      expandedContent = expandedContent.replace(
        new RegExp(`<${componentName}\\s*/>`, 'g'),
        importMap.get(componentName)
      );
      continue;
    }

    // Only look for partial files if this is an expected partial
    if (expectedPartials.has(componentName)) {
      // Try to find the partial file in current directory, parent directory, and docs directory
      const partialFileName = `_${componentName.toLowerCase()}.mdx`;
      const possiblePaths = [
        join(dirname(docPath), partialFileName),
        join(dirname(dirname(docPath)), partialFileName),
        join(rootDir, 'docs', partialFileName),
      ];

      let partialContent = null;
      for (const partialPath of possiblePaths) {
        try {
          partialContent = await readFile(partialPath, 'utf-8');
          // Recursively expand partials in the partial content
          partialContent = await expandPartials(partialContent, partialPath, rootDir, depth + 1);
          break;
        } catch (error) {
          // Continue trying other paths
          continue;
        }
      }

      if (partialContent) {
        // Replace the component usage with its content
        expandedContent = expandedContent.replace(new RegExp(`<${componentName}\\s*/>`, 'g'), partialContent);
      } else {
        console.warn(`Could not find partial file for component: ${componentName}`);
      }
    }
  }

  return expandedContent;
}

export async function writeMarkdown(
  docs: DocsFile[],
  outputFilename: string = 'llms-full.txt',
  silent: boolean = false
): Promise<string> {
  // __dirname is not available in ES modules. Derive the directory of the current module.
  const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../../../');
  const outputPath = join(projectRoot, 'build', outputFilename);
  const rootDir = projectRoot;

  const markdownOutput = await Promise.all(
    docs.map(async doc => {
      // Get the relative path for the URL, starting from the 'docs' directory
      const relativePath = doc.path.split('/docs/').pop() || '';
      const cleanPath = relativePath.replace(/index\.mdx?$/, '').replace(/\.mdx?$/, '');
      const url = `${BASE_URL}/${cleanPath}`;

      // Handle partial imports and expand them recursively
      const content = await expandPartials(doc.content, doc.path, rootDir);

      const heading = doc.title ? `# ${doc.title}\n\n` : '';
      const urlSection = `URL: ${url}\n\n`;
      return `${heading}${urlSection}${content.trim()}\n\n\n\n==============================\n\n\n\n`;
    })
  );

  const finalOutput = markdownOutput.join('');
  // Ensure the build directory exists
  await mkdir(dirname(outputPath), { recursive: true });
  await writeFile(outputPath, finalOutput, 'utf-8');
  if (!silent) {
    console.log(`✅ ${outputFilename} written!`);
  }

  return outputPath;
}

export async function cleanUpImports(pathToMarkdown: string, silent: boolean = false): Promise<void> {
  const content = await readFile(pathToMarkdown, 'utf-8');

  const cleaned = content
    .split('\n')
    .filter(line => {
      // Only remove non-partial imports
      const trimmed = line.trim();
      return !(trimmed.startsWith('import') && !trimmed.includes('_partials'));
    })
    .join('\n');

  await writeFile(pathToMarkdown, cleaned, 'utf-8');
  if (!silent) {
    console.log(`🧹 Imports cleaned up`);
  }
}
