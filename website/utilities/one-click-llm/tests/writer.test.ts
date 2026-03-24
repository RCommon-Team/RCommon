// Jest provides expect & test globals, no import needed
import { mkdir, readFile, writeFile, unlink } from 'fs/promises';
import { tmpdir } from 'os';
import { join } from 'path';
import { writeMarkdown, cleanUpImports } from '../src/writer';

const TEST_OUTPUT_FILE = 'llms-full.test.txt';

test('Writes what we expect', async () => {
  const dir = join(tmpdir(), `write-test-${Date.now()}`);
  await mkdir(dir, { recursive: true });

  const docs = [
    { title: 'Test Title', content: 'Some content here.', path: '/docs/test.md' },
    { title: '', content: 'Another block.', path: '/docs/another.md' },
  ];

  const path = await writeMarkdown(docs as any, TEST_OUTPUT_FILE);
  const result = await readFile(path, 'utf-8');

  expect(result).toContain('# Test Title');
  expect(result).toContain('Another block.');

  // Clean up the test file
  await unlink(path);

  // Log the cleanup
  console.log(`🧹 Cleanup: ${path}`);
});

test('Remove import statements', async () => {
  const dir = join(tmpdir(), `cleanup-test-${Date.now()}`);
  await mkdir(dir, { recursive: true });

  const filePath = join(dir, 'doc.md');
  await writeFile(filePath, `import x from 'y';\nContent\nimport z;`);

  await cleanUpImports(filePath);
  const cleaned = await readFile(filePath, 'utf-8');

  expect(cleaned).not.toContain('import');
  expect(cleaned).toContain('Content');
});
