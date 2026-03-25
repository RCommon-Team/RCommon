import { readFileSync, writeFileSync, readdirSync, statSync } from 'fs';
import { join, relative, basename, extname } from 'path';

const DOCS_DIR = join(__dirname, '..', 'docs');
const OUTPUT_FILE = join(__dirname, '..', 'static', 'llms-full.txt');
const SITE_URL = 'https://rcommon.com';

interface DocFile {
  path: string;
  relativePath: string;
  title: string;
  description: string;
  content: string;
}

function collectMdxFiles(dir: string): string[] {
  const files: string[] = [];
  for (const entry of readdirSync(dir)) {
    const fullPath = join(dir, entry);
    const stat = statSync(fullPath);
    if (stat.isDirectory()) {
      files.push(...collectMdxFiles(fullPath));
    } else if (extname(entry) === '.mdx') {
      files.push(fullPath);
    }
  }
  return files;
}

function parseFrontmatter(content: string): { frontmatter: Record<string, string>; body: string } {
  const match = content.match(/^---\n([\s\S]*?)\n---\n([\s\S]*)$/);
  if (!match) return { frontmatter: {}, body: content };

  const frontmatter: Record<string, string> = {};
  for (const line of match[1].split('\n')) {
    const colonIndex = line.indexOf(':');
    if (colonIndex > 0) {
      const key = line.slice(0, colonIndex).trim();
      const value = line.slice(colonIndex + 1).trim().replace(/^["']|["']$/g, '');
      frontmatter[key] = value;
    }
  }
  return { frontmatter, body: match[2] };
}

function stripMdxSyntax(content: string): string {
  return content
    // Remove import statements
    .replace(/^import\s+.*$/gm, '')
    // Remove JSX self-closing tags like <NuGetInstall packageName="..." />
    .replace(/<[A-Z]\w+[^>]*\/>/g, '')
    // Remove JSX opening+closing tags and their content if single-line
    .replace(/<[A-Z]\w+[^>]*>.*?<\/[A-Z]\w+>/g, '')
    // Remove JSX opening tags (multi-line components)
    .replace(/<[A-Z]\w+[^>]*>/g, '')
    // Remove JSX closing tags
    .replace(/<\/[A-Z]\w+>/g, '')
    // Clean up multiple blank lines
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}

function filePathToUrl(relativePath: string): string {
  const urlPath = relativePath
    .replace(/\\/g, '/')
    .replace(/\/index\.mdx$/, '')
    .replace(/\.mdx$/, '');
  return `${SITE_URL}/docs/${urlPath}`;
}

function main(): void {
  const files = collectMdxFiles(DOCS_DIR);
  const docs: DocFile[] = [];

  for (const filePath of files) {
    const raw = readFileSync(filePath, 'utf-8');
    const { frontmatter, body } = parseFrontmatter(raw);
    const relativePath = relative(DOCS_DIR, filePath);
    const cleanContent = stripMdxSyntax(body);

    docs.push({
      path: filePath,
      relativePath,
      title: frontmatter.title || basename(filePath, '.mdx'),
      description: frontmatter.description || '',
      content: cleanContent,
    });
  }

  // Sort by relative path for consistent ordering
  docs.sort((a, b) => a.relativePath.localeCompare(b.relativePath));

  const sections = docs.map((doc) => {
    const url = filePathToUrl(doc.relativePath);
    const header = `## ${doc.title}`;
    const source = `Source: ${url}`;
    const desc = doc.description ? `\n${doc.description}\n` : '';
    return `${header}\n${source}${desc}\n${doc.content}`;
  });

  const output = `# RCommon — Full Documentation

> This file contains the complete documentation for RCommon, an open-source .NET infrastructure library.
> Generated from source documentation at https://rcommon.com/docs
> For a summary, see https://rcommon.com/llms.txt

---

${sections.join('\n\n---\n\n')}
`;

  writeFileSync(OUTPUT_FILE, output, 'utf-8');
  console.log(`Generated llms-full.txt: ${docs.length} pages, ${(output.length / 1024).toFixed(1)} KB`);
}

main();
