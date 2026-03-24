const fs = require('fs');
const path = require('path');
const OpenAI = require('openai');

require('dotenv').config();

// Get target path and context enhancement from command line arguments
const targetPath = process.argv[2];
const contextEnhancement = process.argv[3] || '';

const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY
});

// Implementing a delay function using setTimeout and Promise
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

// Function to generate prompt for user to update front matter
const promptGenerator = (content, additionalContext) => {
  return `
You are an SEO expert and your task is to improve only the description and keywords for the front matter in YAML format 
of the below page of the documentation site for the Hasura software product. You are to leave all other values as they are. 

For the description:
- The improvements should be relevant to the content on the page
- If the description contains any special characters then output it between double quotes. 
- Do not exceed 320 characters, which is the ideal Google search meta description length.
- Do not take the existing description into account


For the keywords:
- The improvements should be relevant to the content of the page and should likely lead to increased traffic on the 
  page by using relevant and popular keywords. 
- Don't add more than 10 keywords.
- The keywords should all be in lowercase.
- Do not take the existing keywords into account


Only respond with the front matter in YAML format with updated description and keywords between (and including) the 
"---" characters. 

Also include a new property on a new line of: "seoFrontMatterUpdated: true", to indicate that the front matter has 
been updated. If "seoFrontMatterUpdated: false" exists, flip it to true.  

Remember, Only update the description and keywords. Do not update any of the other properties, or change the order in 
which they appear, and don't include any other text except for: "seoFrontMatterUpdated: true". 

Here is an example of a good response:

*** START OF GOOD RESPONSE EXAMPLE ***
---
sidebar_label: Commands
description: "Expand your use of the Hasura Data Domain Specification by learning how to implement commands for executing business logic directly within your GraphQL API. Dive into custom server creation, serverless function utilization and API management."
sidebar_position: 6
keywords:
  - hasura commands
  - hasura dds commands
  - graphql api
  - backend logic
  - metadata
  - hasura data
  - graphql api structure
  - schema
  - data management
  - rest endpoints
hide_table_of_contents: true
toc_max_heading_level: 4
seoFrontMatterUpdated: true
---
*** END OF GOOD RESPONSE EXAMPLE ***

Take into account this additional context for this page (if there is any):

*** START OF ADDITIONAL CONTEXT ***
${additionalContext}
** END OF ADDITIONAL CONTEXT **


Here is the actual content of the page: 
** START OF ACTUAL CONTENT **
${content}
** END OF ACTUAL CONTENT **
`}

// improveSEO function to call OpenAI API and get the improved front matter
async function improveSEO(fileContent) {

  const completion = await openai.chat.completions.create({
    messages: [{ role: 'user', content: promptGenerator(fileContent) }],
    model: 'gpt-4',
  });

  console.log("improved front matter:\n", completion.choices[0].message.content);

  return completion.choices[0].message.content;
}

// Function to extract front matter from the content
function extractFrontMatter(content) {
  const match = content.match(/---\n([\s\S]*?)\n---/);
  if (match) {
    return match[1];
  }
  return null;
}

// Function to process the file and update the front matter
async function processFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf-8');

  // Trim the content to 8000 characters
  const trimmedContent = content.substring(0, 8000);

  const frontMatter = "---\n" + extractFrontMatter(trimmedContent) + "\n---";

  if (frontMatter) {

    if (frontMatter.includes("seoFrontMatterUpdated: true")) {
      console.log(`Skipping ${filePath} as it's already updated.\n`);
      return "SKIPPED";
    }

    const improvedSEO = await improveSEO(trimmedContent);

    const updatedContent = content.replace(/---\n([\s\S]*?)\n---/, `${improvedSEO}`);
    fs.writeFileSync(filePath, updatedContent, 'utf-8');
  }
}

const directoriesToSkip = ['../../docs/cli/commands'];

// Function to traverse the directory or file and process recursively
async function processPath(inputPath) {
  const stats = fs.statSync(inputPath);

  if (stats.isFile() && shouldProcessFile(inputPath)) {
    await processFileWithDelay(inputPath);
  } else if (stats.isDirectory()) {
    // Check if directory is in the list of directories to be skipped
    if (directoriesToSkip.some(directory => inputPath.startsWith(directory))) {
      console.log(`Skipping directory ${inputPath}\n`);
      return;
    }

    const files = fs.readdirSync(inputPath);
    for (const file of files) {
      await processPath(path.join(inputPath, file));
    }
  }
}

function shouldProcessFile(filePath) {
  return path.extname(filePath) === '.mdx' && !path.basename(filePath).startsWith('_');
}

async function processFileWithDelay(filePath) {
  const processResult = await processFile(filePath);
  if (processResult !== "SKIPPED") {
    console.log(`Waiting 2s - API rate limiting... \n`);
    await delay(2000);
    console.log(`Finished waiting. Proceeding \n`);
  }
}


// Do it
processPath("../../docs" + (targetPath ?? ''));
