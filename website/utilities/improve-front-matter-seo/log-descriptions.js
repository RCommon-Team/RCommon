const fs = require('fs');
const path = require('path');

// Function to extract front matter from the content
function extractFrontMatter(content) {
  const match = content.match(/---\n([\s\S]*?)\n---/);
  if (match) {
    return match[1];
  }
  return null;
}

// Function to traverse the directory or file and process recursively
function processPath(inputPath) {
  const stats = fs.statSync(inputPath);

  if (stats.isFile() && path.extname(inputPath) === '.mdx') {
    const content = fs.readFileSync(inputPath, 'utf-8');
    const frontMatter = extractFrontMatter(content);
    const descriptionMatch = frontMatter.match(/description: "(.*?)"/);
    const description = descriptionMatch ? descriptionMatch[1] : 'No description';
    console.log(`File path: ${inputPath}\nDescription: ${description}\n`);
  } else if (stats.isDirectory()) {
    const files = fs.readdirSync(inputPath);
    for (const file of files) {
      processPath(path.join(inputPath, file));
    }
  }
}

// Start processing from the root directory
processPath("../../docs");