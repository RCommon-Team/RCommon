# RCommon.ApiReferenceGenerator

Generates `website/static/api-reference-full.txt`: a single machine-readable file listing every public type and member across all RCommon packages, with signatures and XML doc-comment summaries.

It works by reflecting over the built `RCommon.*.dll` assemblies (via `System.Reflection.MetadataLoadContext`, so nothing is executed) and pairing each member with its corresponding entry in the compiler-generated `.xml` doc-comment sidecar.

## Prerequisite

The Src assemblies must be built in `Release` configuration first, since the generator looks for `Src/<Project>/bin/Release/<tfm>/<AssemblyName>.dll` (and its `.xml` sidecar):

```
dotnet build Src/RCommon.sln -c Release
```

If no built assemblies are found, the tool prints a warning and exits 0 without touching any existing output file — it will not fail `pnpm build`.

## Usage

```
dotnet run --project Tools/RCommon.ApiReferenceGenerator -c Release -- <path-to-Src-directory> <output-file-path>
```

This is wired into the website's build via the `generate:api` script in `website/package.json`, which runs before `docusaurus build`.
