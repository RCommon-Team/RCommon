import { mkdir, writeFile } from 'fs/promises';
import { tmpdir } from 'os';
import { join } from 'path';
import type { DocsFile } from '../src/types';
import {
  extractFiles,
  extractSubdirectories,
  getDocsFilesAndSubDirectories,
  getDocsDirectory,
  getPositionAndContent,
  orderDocFileArray,
  readCategoryPosition,
} from '../src/collator';

test('Find docs directory', async () => {
  const result = await getDocsDirectory('docs');

  expect(result).toContain('/promptql-docs/docs');
});

test('Get the files and subdirs', async () => {
  const docsDirectory = await getDocsDirectory('docs');
  const result = await getDocsFilesAndSubDirectories(docsDirectory);

  // Just an example of one file/directory to include 👇
  expect(result).toContain('auth');
});

test('Extract the files in a docs directory', async () => {
  const docsDirectory = await getDocsDirectory('docs');
  const pageFiles = await extractFiles(docsDirectory);

  expect(Array.isArray(pageFiles)).toBe(true);
});

test('Extract subdirectories in a docs directory', async () => {
  const docsDirectory = await getDocsDirectory('docs');
  const subdirs = await extractSubdirectories(docsDirectory);

  expect(Array.isArray(subdirs)).toBe(true);
});

test('Returns position from valid _category_.json', async () => {
  const testDir = join(tmpdir(), `test-docs-${Date.now()}`);
  await mkdir(testDir, { recursive: true });

  const categoryPath = join(testDir, '_category_.json');
  await writeFile(categoryPath, JSON.stringify({ position: 3 }), 'utf-8');

  const result = await readCategoryPosition(testDir);
  expect(result).toBe(3);
});

test('Order a set of DocFiles', async () => {
  const unsorted: DocsFile[] = [
    { position: [3], content: 'Third', path: '' },
    { position: [1], content: 'First', path: '' },
    { position: [2], content: 'Second', path: '' },
  ];

  const sorted = orderDocFileArray(unsorted);

  expect(sorted.map(f => f.content)).toEqual(['First', 'Second', 'Third']);
});

test('Get sidebar position and content from a file', async () => {
  const testDir = join(tmpdir(), `test-docs-${Date.now()}`);
  await mkdir(testDir, { recursive: true });

  const testFile = join(testDir, 'test.md');
  const content = `---
sidebar_position: 1
title: Test Doc
---

Test content`;
  await writeFile(testFile, content, 'utf-8');

  const docFile = await getPositionAndContent(testFile);

  expect(Array.isArray(docFile?.position)).toBe(true);
  expect(typeof docFile?.position[0]).toBe('number');
});
