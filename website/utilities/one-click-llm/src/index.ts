import { getDocsDirectory, orderDocFileArray, collectDocsRecursive } from '../src/collator';
import { writeMarkdown, cleanUpImports } from './writer';

async function main(silent: boolean = false) {
  const docsDirectory = await getDocsDirectory('docs', silent);
  const docs = await collectDocsRecursive(docsDirectory);
  const sorted = orderDocFileArray(docs);
  const singleMdDocs = await writeMarkdown(sorted, 'llms-full.txt', silent);
  await cleanUpImports(singleMdDocs, silent);
}

// Check if --silent flag is passed
const silent = process.argv.includes('--silent');
await main(silent);
