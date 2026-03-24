import { generatePageMarkdown } from './logic/helpers';
import { fileToObjectsMapping } from './entities/objects';

async function main() {
  // generate markdown for metadata objects
  for (const [fileName, metadataObjectTitles] of Object.entries(fileToObjectsMapping)) {
    generatePageMarkdown(fileName, metadataObjectTitles);
  }
}

main();
