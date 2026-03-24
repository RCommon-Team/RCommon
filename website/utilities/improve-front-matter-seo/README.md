# SEO Front Matter Updater

This script is designed to improve the SEO of the front matter in .mdx files. It uses OpenAI's GPT-4 model to generate
improved keywords and descriptions for the front matter of documentation pages.

## Prerequisites

- Node.js
- An OpenAI API key

## Setup

1. Clone the repository:

```bash
git clone [your-repo-url]
cd [your-repo-directory]
```

### Install the required packages:

```bash
npm install
```

### Set up your environment variables:

Create a .env file in the root directory and add your OpenAI API key:

```makefile
OPENAI_API_KEY=your_openai_api_key
```

## Usage

Run the script using the following command:

```bash
node improve-seo.js [target-path] [additional-text]
```

- [target-path]: The directory or file path where you want to start the traversal. If omitted it will start in /docs.
- [additional-text]: Optional text that you want to add to augment the prompt for the OpenAI model.

### Example:

```bash
node improve-seo.js /supergraph-modeling 'Remember to focus on cloud-related keywords. Or add the copy from
Overview.mdx for the section.'
```

This will start the traversal from the provided directory or file and use the provided additional text to augment the
prompt.

## Features

- Accepts files or traverses directories and updates only .mdx files with front matter.
- Uses OpenAI's GPT-4 model to generate improved keywords and descriptions.
- Skips files that have already been updated.
- Accepts additional text to augment the prompt for the OpenAI model.

## Notes

- Ensure you have a valid OpenAI API key and sufficient quota before running the script.
- GPT-4 is slow. Be patient.
- The script waits for 2 seconds after each API call to avoid hitting rate limits.
- Files that have already been updated with the line `seoFrontMatterUpdated: true` in their front matter will be
  skipped. Delete that line or flip it to `false` to re-run the script on a file.
