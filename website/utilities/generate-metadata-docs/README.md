# Auto-generate Metadata Docs

## Overview

This utility will automatically generate a reference doc for Hasura Metadata using a provided set of JSON schemas. If
examples are provided in the schema, they're transformed into YAML for docs using `js-yaml`.

## Usage

Before running the first time — or when orchestrating CI — ensure you install all necessary dependencies:

```bash
cd utilities/generate-metadata-docs
# if you're running this in an Action, install TS
# npm install typescript
npm i
```

Assuming you have the appropriate files (`hasura_yaml_schema_resolved.json`, `hml_schema_resolved.json`,
`yaml_schema_resolved.json`) living in root of this utility, you can run the following to regenerate the metadata
reference pages in Supergraph Modeling:

```bash
npm run build
npm run start
```

> [!TIP] CI takes care of updating these files **whenever** the golden files are updated in the engine; these then
> trickle their way down via LSP to the CLI and Docs 🚀

## Contributing

If you're more eager to develop with watch mode, and don't like tests (shame on you), run the following:

```bash
npm run watch
```
