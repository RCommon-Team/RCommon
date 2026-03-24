# CLI-Version Update Utility

This utility updates the source file with the latest CLI version listed in the `_install-the-cli.mdx` component.

## Set-up

### Step 1: Create a .env

Inside `uiltities/update-cli-version` add a scaffolded `.env` using the following command:

```sh
echo -e 'V3_CLI_RELEASE_URL="https://api.github.com/repos/hasura/v3-cli-go/releases/latest"\nGH_CLI_VERSION_TOKEN=' > .env
```

### Step 2: Add a GitHub token

You can find a fine-grained token [here](https://share.1password.com/s#bMnGGz-A9E7zA0goYR5XlYaHRX0tHJEH97LMqev5oWI)
which only has read-access to the `v3-cli-go` repository's content. However, you can also add your own token, so long as
SSO is enabled and the correct read-access permissions are granted.

### Step 3: Run the utility

From the root of the docs directory, use the following command to run this utility:

```sh
yarn update-cli-version
```

## How it works

At its heart, the utility simply reaches out to the GitHub API and requests the latest release information for the
`v3-cli-go` repo:

```ts
const getLatestVersion = async (): Promise<string | null> => {
  const URL = process.env.V3_CLI_RELEASE_URL;
  const TOKEN = process.env.GH_CLI_VERSION_TOKEN;

  // We'll appease the compiler of any non-string possibilities
  if (!URL || !TOKEN) {
    throw new Error(
      `Environment variables V3_CLI_RELEASE_URL and GH_CLI_VERSION_TOKEN must be defined in this utility's .env`
    );
  }

  try {
    const response = await fetch(URL, {
      headers: {
        Authorization: `Bearer ${TOKEN}`,
        Accept: 'application/vnd.github+json',
        'X-Github-Api-Version': '2022-11-28',
      },
    });

    if (!response.ok) {
      throw new Error(`Error fetching latest release: ${response.statusText}`);
    }

    const release = await response.json();
    return release.tag_name;
  } catch (error) {
    console.error('Error retrieving latest release:', error);
    return null;
  }
};
```

From there, we simply update a source file and import it to our CLI component:

```tsx
import React from 'react';
import latestVersion from '../../../utilities/update-cli-version/latest-version.json';

const Index: React.FC = () => {
  const version = latestVersion.tag_name;

  console.log(version);

  return (
    <div>
      <p>
        You can download the CLI binary below. The latest version of the CLI is <kbd>{version}</kbd>. Please follow the
        instructions for your system.
      </p>
    </div>
  );
};

export default Index;
```
