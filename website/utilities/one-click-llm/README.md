# One-Click LLM

This utility produces a single markdown file which is a composite of all our raw docs.

## Getting started

This workflow is triggered via an Action so that—whenever we merge a PR to `main`—the output markdown file is added as a
commit before merging.

However, if you'd like to work on this or run it yourself, you'll need to install [Bun](https://bun.sh/).

From there, after navigating to this directory, you can install deps:

```sh
bun install
```

And then either run the script in dev mode:

```sh
bun run dev
```

Or, even better, execute tests as you develop:

```sh
bun run test
```
