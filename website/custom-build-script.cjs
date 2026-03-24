// custom-build-script.cjs
// Consolidated build script that:
// 1. Builds the Docusaurus site
// 2. Generates the llms-full.txt payload via the Bun one-click-llm script
// 3. Copies the generated docs JSON schema into the build folder so that the Console can consume it
// (previously handled by copy-docs-schema-for-console.cjs)

/* eslint-disable no-console */
const path = require('path');
const fs = require('fs');
const { execSync } = require('child_process');
const rootDir = __dirname;

function copyJsonSchema() {
  console.log('\n\x1b[32mTrying to copy the docs JSON schema file to project root for the console...\x1b[0m');
  const docsDir = path.join(__dirname, '.docusaurus/docusaurus-plugin-content-docs/default');

  // First check the default path
  let jsonFile = null;
  let searchPaths = [
    path.join(docsDir, 'p'),
    docsDir,
    path.join(__dirname, '.docusaurus/docusaurus-plugin-content-docs'),
  ];

  // Try each potential path
  for (const searchPath of searchPaths) {
    if (fs.existsSync(searchPath)) {
      console.log(`\x1b[36mSearching in: ${searchPath}\x1b[0m`);
      try {
        const files = fs.readdirSync(searchPath);
        jsonFile = files.find(file => file.startsWith('docs') && file.endsWith('.json'));
        if (jsonFile) {
          console.log(`\x1b[32mFound docs JSON file: ${jsonFile}\x1b[0m`);
          const sourcePath = path.join(searchPath, jsonFile);
          const targetPath = path.join(__dirname, 'build/docs-schema.json');
          fs.copyFileSync(sourcePath, targetPath);
          return true; // Indicate success
        }
      } catch (err) {
        console.warn(`\x1b[33mWarning: Error reading directory ${searchPath}:\x1b[0m`, err?.message || err);
      }
    }
  }

  // If we get here, we couldn't find the file
  console.warn('\x1b[33mWarning: Could not find the docs JSON file in any of the expected locations\x1b[0m');
  console.warn('\x1b[33mThis may affect some console functionality but the build will continue\x1b[0m');
  return false; // Indicate failure
}

function generateLlmBundle() {
  console.log('\n\x1b[36mGenerating llms-full.txt for LLM prompts...\x1b[0m');
  const utilDir = path.join(rootDir, 'utilities', 'one-click-llm');

  try {
    // First install dependencies
    console.log('\x1b[36mInstalling one-click-llm dependencies...\x1b[0m');
    execSync('npm install', { cwd: utilDir, stdio: 'inherit' });

    // Then run the start script
    console.log('\x1b[36mGenerating LLM bundle...\x1b[0m');
    execSync('npm run start -- --silent', { cwd: utilDir, stdio: 'inherit' });
    console.log('\x1b[32mSuccessfully generated LLM bundle!\x1b[0m');
  } catch (err) {
    console.warn('\x1b[33mWarning: Could not generate LLM bundle:\x1b[0m', err?.message || err);
    // Don't throw error to allow build to continue
  }
}

// First run the Docusaurus build
console.log('\n\x1b[36mRunning Docusaurus build...\x1b[0m');
execSync('docusaurus build', { cwd: rootDir, stdio: 'inherit' });

// Then try to generate LLM bundle
try {
  generateLlmBundle();
} catch (e) {
  console.warn('\x1b[33mWarning: LLM bundle generation failed but continuing build...\x1b[0m');
}

// Finally try to copy the JSON schema
try {
  const success = copyJsonSchema();
  if (success) {
    console.log('\x1b[32m\nSuccessfully copied the docs JSON schema file to build assets!\n\x1b[0m');
  }
} catch (e) {
  console.warn('\x1b[33mWarning: Could not copy the docs JSON schema file to build assets:\x1b[0m', e.message);
  console.warn('\x1b[33mThis may affect some console functionality but continuing the build...\x1b[0m');
  // Don't exit with error since this isn't critical
}
