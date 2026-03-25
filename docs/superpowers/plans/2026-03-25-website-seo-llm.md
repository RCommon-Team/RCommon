# Website SEO & LLM Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add comprehensive SEO (meta descriptions, structured data, analytics, missing assets) and LLM discoverability (llms.txt, llms-full.txt) to the RCommon Docusaurus website.

**Architecture:** Docusaurus-native approach — use frontmatter for per-page meta, a custom plugin for JSON-LD, built-in gtag for analytics, static files for crawl directives and LLM content, and a TypeScript build script for llms-full.txt generation.

**Tech Stack:** Docusaurus 3, TypeScript, JSON-LD/schema.org, Google Analytics 4, llms.txt convention

**Spec:** `docs/superpowers/specs/2026-03-25-website-seo-llm-design.md`

---

## File Structure Overview

### New files

```
website/
├── static/
│   ├── robots.txt                        # Crawl directives + sitemap reference
│   ├── img/
│   │   ├── favicon.ico                   # Multi-size favicon (16/32/48px)
│   │   └── og-social-card.jpg            # 1200x630 Open Graph image
│   └── llms.txt                          # Handcrafted LLM discovery file
├── plugins/
│   └── seo-structured-data.ts            # JSON-LD injection plugin
└── scripts/
    └── generate-llms-full.ts             # Build script for llms-full.txt
```

### Modified files

```
website/
├── docusaurus.config.ts                  # gtag, sitemap, headTags, plugin registration
├── package.json                          # generate:llms script, tsx devDep, build script update
├── src/pages/index.tsx                   # SEO-targeted description
└── docs/                                 # description frontmatter on all 62 MDX files
    ├── index.mdx
    ├── getting-started/*.mdx             # 5 files
    ├── core-concepts/*.mdx               # 5 files
    ├── domain-driven-design/*.mdx        # 5 files
    ├── persistence/*.mdx                 # 9 files
    ├── cqrs-mediator/*.mdx               # 5 files
    ├── event-handling/*.mdx              # 7 files
    ├── messaging/*.mdx                   # 5 files
    ├── state-machines/*.mdx              # 2 files
    ├── caching/*.mdx                     # 3 files
    ├── blob-storage/*.mdx                # 3 files
    ├── serialization/*.mdx               # 3 files
    ├── validation/*.mdx                  # 1 file
    ├── email/*.mdx                       # 2 files
    ├── multi-tenancy/*.mdx               # 2 files
    ├── security-web/*.mdx                # 2 files
    ├── architecture-guides/*.mdx         # 3 files
    ├── examples-recipes/*.mdx            # 4 files
    ├── testing/*.mdx                     # 2 files
    └── api-reference/*.mdx               # 3 files
```

---

## Task 1: SEO Foundation — Static Assets & Crawl Directives

Creates the missing favicon, OG image, robots.txt, and verifies the site builds with them.

**Files:**
- Create: `website/static/robots.txt`
- Create: `website/static/img/favicon.ico`
- Create: `website/static/img/og-social-card.jpg`

**Note on image assets:** `favicon.ico` and `og-social-card.jpg` are binary files. The implementer must generate them from the existing branding assets. The favicon should be generated from `website/static/img/rcommon-logo.png` using an ICO converter (e.g., ImageMagick `convert` or an online tool like favicon.io). The OG card should be a 1200x630 JPG with the RCommon logo centered, project name "RCommon", tagline "Build Enterprise Applications Without Reinventing the Wheel", on the brand dark background (`#0f172a`). Use any image tool (Figma, Canva, ImageMagick, sharp).

- [ ] **Step 1: Create robots.txt**

Create `website/static/robots.txt`:

```
User-agent: *
Allow: /

Sitemap: https://rcommon.com/sitemap.xml
```

- [ ] **Step 2: Generate favicon.ico**

Generate a multi-size ICO file (16x16, 32x32, 48x48) from `website/static/img/rcommon-logo.png`. Place at `website/static/img/favicon.ico`.

Verify it exists and the config reference in `docusaurus.config.ts` line 8 (`favicon: 'img/favicon.ico'`) will resolve.

- [ ] **Step 3: Generate og-social-card.jpg**

Create a 1200x630 JPG image with:
- Background: `#0f172a` (brand dark)
- RCommon logo centered (from `rcommon-logo-dark-bg.png`)
- Text: "RCommon" and tagline "Build Enterprise Applications Without Reinventing the Wheel"
- Brand blue accent: `#2563eb`

Place at `website/static/img/og-social-card.jpg`. Verify the config reference on `docusaurus.config.ts` line 56 (`image: 'img/og-social-card.jpg'`) will resolve.

- [ ] **Step 4: Verify site builds**

Run from `website/`:
```bash
pnpm build
```

Expected: Build succeeds. Check `website/build/robots.txt` exists, `website/build/img/favicon.ico` exists, `website/build/img/og-social-card.jpg` exists.

- [ ] **Step 5: Commit**

```bash
git add website/static/robots.txt website/static/img/favicon.ico website/static/img/og-social-card.jpg
git commit -m "feat(seo): add robots.txt, favicon, and OG social card image"
```

---

## Task 2: Docusaurus Config — Sitemap, Analytics & Search Console

Configures explicit sitemap settings, GA4 gtag (placeholder), and Search Console verification placeholder.

**Files:**
- Modify: `website/docusaurus.config.ts`

- [ ] **Step 1: Add gtag configuration to preset**

In `website/docusaurus.config.ts`, add `gtag` to the classic preset options (after the `theme` key, around line 35):

```typescript
gtag: {
  trackingID: 'G-XXXXXXXXXX', // TODO: Replace with real GA4 Measurement ID
  anonymizeIP: true,
},
```

The full preset block becomes:
```typescript
presets: [
  [
    'classic',
    {
      docs: {
        sidebarPath: './sidebars.ts',
        editUrl: 'https://github.com/RCommon-Team/RCommon/tree/main/website/',
      },
      blog: false,
      theme: {
        customCss: './src/css/custom.css',
      },
      gtag: {
        trackingID: 'G-XXXXXXXXXX', // TODO: Replace with real GA4 Measurement ID
        anonymizeIP: true,
      },
      sitemap: {
        changefreq: 'weekly',
        priority: 0.5,
        ignorePatterns: ['/tags/**'],
        filename: 'sitemap.xml',
      },
    } satisfies Preset.Options,
  ],
],
```

- [ ] **Step 2: Add headTags for Search Console verification**

Add a top-level `headTags` property to the config object (after `i18n`, around line 20):

```typescript
headTags: [
  {
    tagName: 'meta',
    attributes: {
      name: 'google-site-verification',
      content: 'YOUR_VERIFICATION_CODE', // TODO: Replace with real Search Console verification code
    },
  },
],
```

- [ ] **Step 3: Verify site builds**

Run from `website/`:
```bash
pnpm build
```

Expected: Build succeeds. Check that `website/build/sitemap.xml` exists and contains doc page URLs.

- [ ] **Step 4: Commit**

```bash
git add website/docusaurus.config.ts
git commit -m "feat(seo): configure GA4 gtag, sitemap, and Search Console placeholders"
```

---

## Task 3: Structured Data Plugin — JSON-LD

Creates a custom Docusaurus plugin that injects JSON-LD structured data into page `<head>` tags.

**Files:**
- Create: `website/plugins/seo-structured-data.ts`
- Modify: `website/src/theme/DocItem/Metadata/index.tsx`
- Modify: `website/docusaurus.config.ts` (register plugin)

**Approach:** Two-part strategy:
1. A plugin using `injectHtmlTags` adds `SoftwareApplication` JSON-LD globally (it describes the software, applies to every page).
2. The existing swizzled `DocItem/Metadata/index.tsx` already has access to `useDoc()` with title/description/permalink — add a `<Head>` component there to inject per-page `TechArticle` JSON-LD.

- [ ] **Step 1: Create the plugin file**

Create `website/plugins/seo-structured-data.ts`:

```typescript
import type { Plugin } from '@docusaurus/types';

const SOFTWARE_APPLICATION_JSONLD = {
  '@context': 'https://schema.org',
  '@type': 'SoftwareApplication',
  name: 'RCommon',
  description:
    'Open-source .NET infrastructure library providing battle-tested abstractions for persistence, CQRS, event handling, messaging, caching, and more.',
  url: 'https://rcommon.com',
  applicationCategory: 'DeveloperApplication',
  operatingSystem: '.NET 8, .NET 9, .NET 10',
  license: 'https://opensource.org/licenses/Apache-2.0',
  offers: {
    '@type': 'Offer',
    price: '0',
    priceCurrency: 'USD',
  },
  sourceOrganization: {
    '@type': 'Organization',
    name: 'RCommon Team',
    url: 'https://github.com/RCommon-Team',
  },
};

export default function seoStructuredDataPlugin(): Plugin {
  return {
    name: 'seo-structured-data',
    injectHtmlTags() {
      return {
        headTags: [
          {
            tagName: 'script',
            attributes: {
              type: 'application/ld+json',
            },
            innerHTML: JSON.stringify(SOFTWARE_APPLICATION_JSONLD),
          },
        ],
      };
    },
  };
}
```

- [ ] **Step 2: Add TechArticle JSON-LD to DocItem/Metadata**

Modify `website/src/theme/DocItem/Metadata/index.tsx`. This file is already swizzled and has access to `useDoc()` with title, description, and permalink. Add a `<Head>` component to inject per-page `TechArticle` JSON-LD.

Replace the file contents with:

```tsx
import React from 'react';
import Head from '@docusaurus/Head';
import {PageMetadata} from '@docusaurus/theme-common';
import {useDoc} from '@docusaurus/theme-common/internal';

export default function DocItemMetadata(): JSX.Element {
  const {metadata, frontMatter, assets} = useDoc();

  const techArticleJsonLd = {
    '@context': 'https://schema.org',
    '@type': 'TechArticle',
    headline: metadata.title,
    description: metadata.description,
    url: `https://rcommon.com${metadata.permalink}`,
    publisher: {
      '@type': 'Organization',
      name: 'RCommon Team',
    },
    about: {
      '@type': 'SoftwareApplication',
      name: 'RCommon',
    },
  };

  return (
    <>
      <PageMetadata
        title={metadata.title}
        description={metadata.description}
        keywords={frontMatter.keywords}
        image={assets.image ?? frontMatter.image}
      />
      <Head>
        <script type="application/ld+json">
          {JSON.stringify(techArticleJsonLd)}
        </script>
      </Head>
    </>
  );
}
```

- [ ] **Step 3: Register the plugin in docusaurus.config.ts**

Add the plugin to the `plugins` array in `website/docusaurus.config.ts` (alongside the existing tailwind plugin). Docusaurus handles `.ts` plugin files natively when the project has TypeScript configured (which this project does via `@docusaurus/tsconfig`):

```typescript
plugins: [
  async function tailwindPlugin(context, options) {
    return {
      name: 'tailwind-plugin',
      configurePostCss(postcssOptions) {
        postcssOptions.plugins.push(require('tailwindcss'));
        postcssOptions.plugins.push(require('autoprefixer'));
        return postcssOptions;
      },
    };
  },
  './plugins/seo-structured-data',
],
```

**Note:** Use the string path `'./plugins/seo-structured-data'` (not `require.resolve()`). Docusaurus resolves plugin paths relative to the site directory and handles TypeScript transpilation automatically.

- [ ] **Step 4: Verify site builds and check JSON-LD output**

Run from `website/`:
```bash
pnpm build
```

Expected: Build succeeds. Then verify JSON-LD is present:

```bash
# Check homepage has SoftwareApplication
grep -l "SoftwareApplication" website/build/index.html

# Check a doc page has TechArticle
grep -l "TechArticle" website/build/docs/getting-started/overview/index.html
```

Both should return file paths (match found).

- [ ] **Step 5: Commit**

```bash
git add website/plugins/seo-structured-data.ts website/src/theme/DocItem/Metadata/index.tsx website/docusaurus.config.ts
git commit -m "feat(seo): add JSON-LD structured data for SoftwareApplication and TechArticle"
```

---

## Task 4: Per-Page Meta Descriptions — Getting Started & Core Concepts

Adds `description` frontmatter to the first batch of MDX files: docs index, getting-started (5), and core-concepts (5) — 11 files total.

**Files:**
- Modify: `website/docs/index.mdx`
- Modify: `website/docs/getting-started/overview.mdx`
- Modify: `website/docs/getting-started/installation.mdx`
- Modify: `website/docs/getting-started/quick-start.mdx`
- Modify: `website/docs/getting-started/configuration.mdx`
- Modify: `website/docs/getting-started/dependency-injection.mdx`
- Modify: `website/docs/core-concepts/fluent-configuration.mdx`
- Modify: `website/docs/core-concepts/guards.mdx`
- Modify: `website/docs/core-concepts/guid-generation.mdx`
- Modify: `website/docs/core-concepts/system-time.mdx`
- Modify: `website/docs/core-concepts/execution-results.mdx`

**Process for each file:** Read the file content. Write a unique 150-160 character description summarizing what the page covers. Add `description` as a frontmatter field after `sidebar_position`.

- [ ] **Step 1: Add descriptions to docs index and getting-started files**

Read each file, then add `description` to frontmatter. Example descriptions (read actual content to refine):

`index.mdx`:
```yaml
description: "Complete documentation for RCommon — a .NET infrastructure library with persistence, CQRS, event handling, messaging, and caching abstractions."
```

`getting-started/overview.mdx`:
```yaml
description: "Learn what RCommon provides — pluggable .NET abstractions for persistence, CQRS, events, messaging, and caching with a fluent DI-first configuration."
```

`getting-started/installation.mdx`:
```yaml
description: "Install RCommon NuGet packages for your .NET 8, 9, or 10 project. Choose from 37+ packages for persistence, CQRS, messaging, and more."
```

`getting-started/quick-start.mdx`:
```yaml
description: "Get started with RCommon in minutes — configure persistence, CQRS, and event handling with a single fluent builder chain in Program.cs."
```

`getting-started/configuration.mdx`:
```yaml
description: "Configure RCommon using the fluent AddRCommon() builder — register providers for persistence, mediator, events, caching, and validation."
```

`getting-started/dependency-injection.mdx`:
```yaml
description: "How RCommon integrates with Microsoft.Extensions.DependencyInjection — service registration, scoping, and provider resolution patterns."
```

- [ ] **Step 2: Add descriptions to core-concepts files**

`core-concepts/fluent-configuration.mdx`:
```yaml
description: "Deep dive into RCommon's fluent configuration API — composable builder pattern for registering providers and customizing behavior."
```

`core-concepts/guards.mdx`:
```yaml
description: "Use RCommon Guard clauses for defensive programming — parameter validation, null checks, and precondition enforcement in .NET."
```

`core-concepts/guid-generation.mdx`:
```yaml
description: "RCommon's IGuidGenerator abstraction for testable, deterministic GUID generation with sequential and custom strategies."
```

`core-concepts/system-time.mdx`:
```yaml
description: "Use ISystemTime to abstract DateTime.Now for testable time-dependent code — freeze, advance, and control time in tests."
```

`core-concepts/execution-results.mdx`:
```yaml
description: "Return success or failure from operations using RCommon's ExecutionResult pattern — structured error handling without exceptions."
```

- [ ] **Step 3: Verify build**

```bash
cd website && pnpm build
```

Expected: Build succeeds with no warnings about missing descriptions.

- [ ] **Step 4: Commit**

```bash
git add website/docs/index.mdx website/docs/getting-started/ website/docs/core-concepts/
git commit -m "feat(seo): add meta descriptions to docs index, getting-started, and core-concepts"
```

---

## Task 5: Per-Page Meta Descriptions — Domain & Persistence

Adds `description` frontmatter to domain-driven-design (5) and persistence (9) — 14 files.

**Files:**
- Modify: `website/docs/domain-driven-design/entities-aggregates.mdx`
- Modify: `website/docs/domain-driven-design/domain-events.mdx`
- Modify: `website/docs/domain-driven-design/value-objects.mdx`
- Modify: `website/docs/domain-driven-design/auditing.mdx`
- Modify: `website/docs/domain-driven-design/soft-delete.mdx`
- Modify: `website/docs/persistence/repository-pattern.mdx`
- Modify: `website/docs/persistence/specifications.mdx`
- Modify: `website/docs/persistence/unit-of-work.mdx`
- Modify: `website/docs/persistence/efcore.mdx`
- Modify: `website/docs/persistence/dapper.mdx`
- Modify: `website/docs/persistence/linq2db.mdx`
- Modify: `website/docs/persistence/sagas.mdx`
- Modify: `website/docs/persistence/caching-memory.mdx`
- Modify: `website/docs/persistence/caching-redis.mdx`

- [ ] **Step 1: Add descriptions to domain-driven-design files**

Read each file, write a unique 150-160 char description. Examples:

`entities-aggregates.mdx`:
```yaml
description: "Define entities and aggregates in RCommon — base classes with identity, equality, domain events, and aggregate root boundaries."
```

`domain-events.mdx`:
```yaml
description: "Raise and handle domain events in RCommon — transactional event dispatching coordinated with the unit of work lifecycle."
```

`value-objects.mdx`:
```yaml
description: "Implement value objects with RCommon's base class — structural equality, immutability, and self-validation patterns for .NET."
```

`auditing.mdx`:
```yaml
description: "Automatic audit fields in RCommon entities — CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate tracked transparently."
```

`soft-delete.mdx`:
```yaml
description: "Soft delete support in RCommon entities — automatic IsDeleted filtering in queries with EF Core global query filters."
```

- [ ] **Step 2: Add descriptions to persistence files**

Read each file, write unique descriptions. Examples:

`repository-pattern.mdx`:
```yaml
description: "RCommon's repository abstractions — ILinqRepository, IReadOnlyRepository, IWriteOnlyRepository, and IGraphRepository interfaces."
```

`efcore.mdx`:
```yaml
description: "Configure EF Core as your RCommon persistence provider with repository pattern, LINQ queries, eager loading, and multi-DbContext support."
```

`dapper.mdx`:
```yaml
description: "Use Dapper as your RCommon persistence provider — lightweight SQL with repository abstraction and unit of work integration."
```

Read actual content of each remaining file (`specifications.mdx`, `unit-of-work.mdx`, `linq2db.mdx`, `sagas.mdx`, `caching-memory.mdx`, `caching-redis.mdx`) and write descriptions accordingly.

- [ ] **Step 3: Verify build**

```bash
cd website && pnpm build
```

- [ ] **Step 4: Commit**

```bash
git add website/docs/domain-driven-design/ website/docs/persistence/
git commit -m "feat(seo): add meta descriptions to domain-driven-design and persistence docs"
```

---

## Task 6: Per-Page Meta Descriptions — CQRS, Events, Messaging

Adds `description` frontmatter to cqrs-mediator (5), event-handling (7), messaging (5) — 17 files.

**Files:**
- Modify: All files in `website/docs/cqrs-mediator/`
- Modify: All files in `website/docs/event-handling/`
- Modify: All files in `website/docs/messaging/`

- [ ] **Step 1: Add descriptions to cqrs-mediator files**

Read each file, write unique 150-160 char descriptions. Examples:

`command-query-bus.mdx`:
```yaml
description: "RCommon's command and query bus — a thin IMediator abstraction for CQRS with pluggable MediatR and Wolverine backends."
```

`commands-handlers.mdx`:
```yaml
description: "Define commands and handlers in RCommon — request/response patterns for write operations through the mediator pipeline."
```

`queries-handlers.mdx`:
```yaml
description: "Define queries and handlers in RCommon — read-side CQRS patterns for retrieving data through the mediator pipeline."
```

`mediatr.mdx`:
```yaml
description: "Configure MediatR as your RCommon CQRS mediator — pipeline behaviors, request handlers, and notification support."
```

`wolverine.mdx`:
```yaml
description: "Configure Wolverine as your RCommon CQRS mediator — message handling, middleware pipeline, and durable messaging support."
```

- [ ] **Step 2: Add descriptions to event-handling files**

Read each file and write descriptions. The 7 files are: `overview.mdx`, `in-memory.mdx`, `distributed.mdx`, `transactional-outbox.mdx`, `mediatr.mdx`, `masstransit.mdx`, `wolverine.mdx`.

- [ ] **Step 3: Add descriptions to messaging files**

Read each file and write descriptions. The 5 files are: `overview.mdx`, `transactional-outbox.mdx`, `state-machines.mdx`, `masstransit.mdx`, `wolverine.mdx`.

- [ ] **Step 4: Verify build**

```bash
cd website && pnpm build
```

- [ ] **Step 5: Commit**

```bash
git add website/docs/cqrs-mediator/ website/docs/event-handling/ website/docs/messaging/
git commit -m "feat(seo): add meta descriptions to CQRS, event-handling, and messaging docs"
```

---

## Task 7: Per-Page Meta Descriptions — Remaining Sections

Adds `description` frontmatter to all remaining doc sections — 20 files total.

**Files:**
- Modify: `website/docs/state-machines/overview.mdx`, `stateless.mdx` (2)
- Modify: `website/docs/caching/overview.mdx`, `memory.mdx`, `redis.mdx` (3)
- Modify: `website/docs/blob-storage/overview.mdx`, `azure.mdx`, `s3.mdx` (3)
- Modify: `website/docs/serialization/overview.mdx`, `newtonsoft.mdx`, `system-text-json.mdx` (3)
- Modify: `website/docs/validation/fluent-validation.mdx` (1)
- Modify: `website/docs/email/overview.mdx`, `sendgrid.mdx` (2)
- Modify: `website/docs/multi-tenancy/overview.mdx`, `finbuckle.mdx` (2)
- Modify: `website/docs/security-web/authorization.mdx`, `web-utilities.mdx` (2)

- [ ] **Step 1: Add descriptions to state-machines, caching, blob-storage**

Read each file and write unique 150-160 char descriptions.

- [ ] **Step 2: Add descriptions to serialization, validation, email**

Read each file and write descriptions.

- [ ] **Step 3: Add descriptions to multi-tenancy, security-web**

Read each file and write descriptions.

- [ ] **Step 4: Verify build**

```bash
cd website && pnpm build
```

- [ ] **Step 5: Commit**

```bash
git add website/docs/state-machines/ website/docs/caching/ website/docs/blob-storage/ website/docs/serialization/ website/docs/validation/ website/docs/email/ website/docs/multi-tenancy/ website/docs/security-web/
git commit -m "feat(seo): add meta descriptions to remaining infrastructure docs"
```

---

## Task 8: Per-Page Meta Descriptions — Guides, Examples, Testing, API Reference

Adds `description` frontmatter to the final batch — 12 files.

**Files:**
- Modify: `website/docs/architecture-guides/clean-architecture.mdx`, `microservices.mdx`, `event-driven.mdx` (3)
- Modify: `website/docs/examples-recipes/hr-leave-management.mdx`, `event-handling.mdx`, `caching.mdx`, `messaging.mdx` (4)
- Modify: `website/docs/testing/overview.mdx`, `test-base-classes.mdx` (2)
- Modify: `website/docs/api-reference/nuget-packages.mdx`, `changelog.mdx`, `migration-guide.mdx` (3)

- [ ] **Step 1: Add descriptions to architecture-guides**

Read each file and write descriptions. Examples:

`clean-architecture.mdx`:
```yaml
description: "How RCommon fits into Clean Architecture — layer boundaries, dependency rules, and mapping abstractions to infrastructure."
```

`microservices.mdx`:
```yaml
description: "Building microservices with RCommon — messaging, event handling, and persistence patterns for distributed .NET systems."
```

`event-driven.mdx`:
```yaml
description: "Event-driven architecture with RCommon — domain events, transactional outbox, and distributed messaging across bounded contexts."
```

- [ ] **Step 2: Add descriptions to examples-recipes, testing, api-reference**

Read each file and write descriptions.

- [ ] **Step 3: Verify build**

```bash
cd website && pnpm build
```

- [ ] **Step 4: Verify all pages have descriptions**

Run a check that no MDX file in `website/docs/` is missing a `description` field:

```bash
# Find MDX files WITHOUT a description frontmatter field
grep -rL "^description:" website/docs/ --include="*.mdx"
```

Expected: No output (all files have descriptions). If any files are listed, add descriptions to them.

- [ ] **Step 5: Commit**

```bash
git add website/docs/architecture-guides/ website/docs/examples-recipes/ website/docs/testing/ website/docs/api-reference/
git commit -m "feat(seo): add meta descriptions to guides, examples, testing, and API reference"
```

---

## Task 9: Homepage SEO Improvement

Updates the homepage `<Layout>` description to be more SEO-targeted than the generic tagline.

**Files:**
- Modify: `website/src/pages/index.tsx:132`

- [ ] **Step 1: Update Layout description**

In `website/src/pages/index.tsx`, change line 132 from:

```tsx
<Layout title={siteConfig.title} description={siteConfig.tagline}>
```

To:

```tsx
<Layout
  title="RCommon — Open Source .NET Infrastructure Library"
  description="RCommon provides pluggable .NET abstractions for persistence, CQRS, event handling, messaging, caching, and more. EF Core, MediatR, MassTransit, Wolverine, Redis — swap providers without touching your domain code."
>
```

- [ ] **Step 2: Verify build**

```bash
cd website && pnpm build
```

Check `website/build/index.html` contains the new description in `<meta name="description">`.

- [ ] **Step 3: Commit**

```bash
git add website/src/pages/index.tsx
git commit -m "feat(seo): improve homepage meta title and description for search engines"
```

---

## Task 10: llms.txt — Handcrafted LLM Discovery File

Creates the curated `llms.txt` file for AI assistant discoverability.

**Files:**
- Create: `website/static/llms.txt`

- [ ] **Step 1: Create llms.txt**

Create `website/static/llms.txt` following the llms.txt convention:

```markdown
# RCommon

> Open-source .NET infrastructure library providing battle-tested abstractions for persistence, CQRS, event handling, messaging, caching, and more. Swap providers (EF Core, Dapper, MediatR, MassTransit, Wolverine, Redis) without touching your domain code.

## Key Facts

- License: Apache 2.0
- Targets: .NET 8, .NET 9, .NET 10
- 37+ NuGet packages with pluggable provider model
- Fluent DI-first configuration via single AddRCommon() builder chain
- GitHub: https://github.com/RCommon-Team/RCommon
- NuGet: https://www.nuget.org/profiles/RCommon

## Core Abstractions

- [Persistence](https://rcommon.com/docs/category/persistence): Repository pattern, Unit of Work, Specifications — EF Core, Dapper, Linq2Db providers
- [CQRS & Mediator](https://rcommon.com/docs/category/cqrs--mediator): Command/Query Bus with MediatR and Wolverine implementations
- [Event Handling](https://rcommon.com/docs/category/event-handling): In-memory and distributed events with transactional outbox support
- [Messaging](https://rcommon.com/docs/category/messaging): Message bus with MassTransit and Wolverine, state machines
- [Caching](https://rcommon.com/docs/category/caching): Unified caching abstraction with Memory and Redis providers
- [Domain-Driven Design](https://rcommon.com/docs/category/domain-driven-design): Entities, Aggregates, Domain Events, Value Objects, Auditing, Soft Delete
- [Blob Storage](https://rcommon.com/docs/category/blob-storage): Azure Blob Storage and Amazon S3 behind a unified abstraction
- [Multi-Tenancy](https://rcommon.com/docs/category/multi-tenancy): Tenant resolution and isolation with Finbuckle integration
- [Serialization](https://rcommon.com/docs/category/serialization): Newtonsoft.Json and System.Text.Json behind a common interface
- [Validation](https://rcommon.com/docs/validation/fluent-validation): FluentValidation integration
- [Email](https://rcommon.com/docs/category/email): SMTP and SendGrid providers
- [State Machines](https://rcommon.com/docs/category/state-machines): Stateless library integration

## Getting Started

- [Overview](https://rcommon.com/docs/getting-started/overview): What RCommon provides and its philosophy
- [Installation](https://rcommon.com/docs/getting-started/installation): NuGet package installation
- [Quick Start](https://rcommon.com/docs/getting-started/quick-start): Get running in minutes
- [Configuration](https://rcommon.com/docs/getting-started/configuration): Fluent builder API reference

## Architecture Guides

- [Clean Architecture](https://rcommon.com/docs/architecture-guides/clean-architecture): Layer boundaries and dependency rules
- [Microservices](https://rcommon.com/docs/architecture-guides/microservices): Distributed systems patterns
- [Event-Driven](https://rcommon.com/docs/architecture-guides/event-driven): Domain events across bounded contexts

## Full Documentation

For comprehensive documentation suitable for RAG and detailed reference:
- [Full docs (text)](https://rcommon.com/llms-full.txt)
- [Website](https://rcommon.com/docs)
```

- [ ] **Step 2: Verify build**

```bash
cd website && pnpm build
```

Check `website/build/llms.txt` exists and is served correctly.

- [ ] **Step 3: Commit**

```bash
git add website/static/llms.txt
git commit -m "feat(llm): add llms.txt for AI assistant discoverability"
```

---

## Task 11: llms-full.txt — Build Script Generator

Creates a TypeScript build script that generates `llms-full.txt` from all MDX documentation content.

**Files:**
- Create: `website/scripts/generate-llms-full.ts`
- Modify: `website/package.json` (add script, add tsx devDep)

- [ ] **Step 1: Install tsx as a dev dependency**

```bash
cd website && pnpm add -D tsx
```

- [ ] **Step 2: Create the generator script**

Create `website/scripts/generate-llms-full.ts`:

```typescript
import { readFileSync, writeFileSync, readdirSync, statSync } from 'fs';
import { join, relative, basename, extname } from 'path';

const DOCS_DIR = join(__dirname, '..', 'docs');
const OUTPUT_FILE = join(__dirname, '..', 'static', 'llms-full.txt');
const SITE_URL = 'https://rcommon.com';

interface DocFile {
  path: string;
  relativePath: string;
  title: string;
  description: string;
  content: string;
}

function collectMdxFiles(dir: string): string[] {
  const files: string[] = [];
  for (const entry of readdirSync(dir)) {
    const fullPath = join(dir, entry);
    const stat = statSync(fullPath);
    if (stat.isDirectory()) {
      files.push(...collectMdxFiles(fullPath));
    } else if (extname(entry) === '.mdx') {
      files.push(fullPath);
    }
  }
  return files;
}

function parseFrontmatter(content: string): { frontmatter: Record<string, string>; body: string } {
  const match = content.match(/^---\n([\s\S]*?)\n---\n([\s\S]*)$/);
  if (!match) return { frontmatter: {}, body: content };

  const frontmatter: Record<string, string> = {};
  for (const line of match[1].split('\n')) {
    const colonIndex = line.indexOf(':');
    if (colonIndex > 0) {
      const key = line.slice(0, colonIndex).trim();
      const value = line.slice(colonIndex + 1).trim().replace(/^["']|["']$/g, '');
      frontmatter[key] = value;
    }
  }
  return { frontmatter, body: match[2] };
}

function stripMdxSyntax(content: string): string {
  return content
    // Remove import statements
    .replace(/^import\s+.*$/gm, '')
    // Remove JSX self-closing tags like <NuGetInstall packageName="..." />
    .replace(/<[A-Z]\w+[^>]*\/>/g, '')
    // Remove JSX opening+closing tags and their content if single-line
    .replace(/<[A-Z]\w+[^>]*>.*?<\/[A-Z]\w+>/g, '')
    // Remove JSX opening tags (multi-line components)
    .replace(/<[A-Z]\w+[^>]*>/g, '')
    // Remove JSX closing tags
    .replace(/<\/[A-Z]\w+>/g, '')
    // Clean up multiple blank lines
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}

function filePathToUrl(relativePath: string): string {
  const urlPath = relativePath
    .replace(/\\/g, '/')
    .replace(/\/index\.mdx$/, '')
    .replace(/\.mdx$/, '');
  return `${SITE_URL}/docs/${urlPath}`;
}

function main(): void {
  const files = collectMdxFiles(DOCS_DIR);
  const docs: DocFile[] = [];

  for (const filePath of files) {
    const raw = readFileSync(filePath, 'utf-8');
    const { frontmatter, body } = parseFrontmatter(raw);
    const relativePath = relative(DOCS_DIR, filePath);
    const cleanContent = stripMdxSyntax(body);

    docs.push({
      path: filePath,
      relativePath,
      title: frontmatter.title || basename(filePath, '.mdx'),
      description: frontmatter.description || '',
      content: cleanContent,
    });
  }

  // Sort by relative path for consistent ordering
  docs.sort((a, b) => a.relativePath.localeCompare(b.relativePath));

  const sections = docs.map((doc) => {
    const url = filePathToUrl(doc.relativePath);
    const header = `## ${doc.title}`;
    const source = `Source: ${url}`;
    const desc = doc.description ? `\n${doc.description}\n` : '';
    return `${header}\n${source}${desc}\n${doc.content}`;
  });

  const output = `# RCommon — Full Documentation

> This file contains the complete documentation for RCommon, an open-source .NET infrastructure library.
> Generated from source documentation at https://rcommon.com/docs
> For a summary, see https://rcommon.com/llms.txt

---

${sections.join('\n\n---\n\n')}
`;

  writeFileSync(OUTPUT_FILE, output, 'utf-8');
  console.log(`Generated llms-full.txt: ${docs.length} pages, ${(output.length / 1024).toFixed(1)} KB`);
}

main();
```

- [ ] **Step 3: Add scripts to package.json**

In `website/package.json`, add the `generate:llms` script and update the `build` script:

```json
"scripts": {
  "generate:llms": "tsx scripts/generate-llms-full.ts",
  "build": "pnpm generate:llms && docusaurus build",
  ...
}
```

- [ ] **Step 4: Run the generator and verify output**

```bash
cd website && pnpm generate:llms
```

Expected: Console output like `Generated llms-full.txt: 62 pages, XXX KB`. Check `website/static/llms-full.txt` exists and contains documentation content without JSX artifacts.

```bash
# Verify no JSX artifacts leaked through
grep -c "<NuGetInstall\|<ProviderComparison\|^import " website/static/llms-full.txt
```

Expected: `0` (no matches).

- [ ] **Step 5: Verify full build**

```bash
cd website && pnpm build
```

Expected: Build succeeds. `website/build/llms-full.txt` exists.

- [ ] **Step 6: Commit**

```bash
git add website/scripts/generate-llms-full.ts website/package.json website/static/llms-full.txt
git commit -m "feat(llm): add build script to generate llms-full.txt from documentation"
```

---

## Task 12: Semantic Link Audit

Reviews all internal links across docs for generic anchor text and replaces with descriptive alternatives.

**Files:**
- Modify: Any MDX files found to have generic link text

- [ ] **Step 1: Search for generic anchor text patterns**

```bash
# Search for common generic link text patterns in MDX files
grep -rn "\[click here\]\|\[here\]\|\[this\]\|\[see this\]\|\[link\]\|\[read more\]" website/docs/ --include="*.mdx" -i
```

Expected: Either no results (all links are already descriptive) or a list of files/lines to fix.

- [ ] **Step 2: Fix any generic links found**

For each result from Step 1, read the file, understand the link context, and replace the generic text with a descriptive alternative. For example:

Before: `[click here](/docs/persistence/efcore)`
After: `[EF Core persistence provider](/docs/persistence/efcore)`

Before: `see [this](/docs/getting-started/configuration) for details`
After: `see the [fluent configuration guide](/docs/getting-started/configuration) for details`

- [ ] **Step 3: Verify build**

```bash
cd website && pnpm build
```

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add website/docs/
git commit -m "feat(seo): replace generic link text with descriptive anchors"
```

---

## Task 13: Final Verification & Squash

Full build verification and commit squash into a single meaningful commit.

**Files:** None (verification only)

- [ ] **Step 1: Full clean build**

```bash
cd website && pnpm clear && pnpm build
```

Expected: Build succeeds with no errors.

- [ ] **Step 2: Verify all deliverables**

Run the following checks against the build output:

```bash
# 1. robots.txt
test -f website/build/robots.txt && echo "PASS: robots.txt" || echo "FAIL: robots.txt"

# 2. favicon
test -f website/build/img/favicon.ico && echo "PASS: favicon" || echo "FAIL: favicon"

# 3. OG image
test -f website/build/img/og-social-card.jpg && echo "PASS: OG image" || echo "FAIL: OG image"

# 4. sitemap
test -f website/build/sitemap.xml && echo "PASS: sitemap" || echo "FAIL: sitemap"

# 5. llms.txt
test -f website/build/llms.txt && echo "PASS: llms.txt" || echo "FAIL: llms.txt"

# 6. llms-full.txt
test -f website/build/llms-full.txt && echo "PASS: llms-full.txt" || echo "FAIL: llms-full.txt"

# 7. JSON-LD on homepage
grep -q "SoftwareApplication" website/build/index.html && echo "PASS: homepage JSON-LD" || echo "FAIL: homepage JSON-LD"

# 8. JSON-LD on doc page
grep -q "TechArticle" website/build/docs/getting-started/overview/index.html && echo "PASS: doc JSON-LD" || echo "FAIL: doc JSON-LD"

# 9. GA4 gtag script reference
grep -q "gtag" website/build/index.html && echo "PASS: GA4 gtag" || echo "FAIL: GA4 gtag"

# 10. All MDX files have descriptions
MISSING=$(grep -rL "^description:" website/docs/ --include="*.mdx")
test -z "$MISSING" && echo "PASS: all descriptions" || echo "FAIL: missing descriptions in: $MISSING"
```

All 10 checks should PASS.

- [ ] **Step 3: Rebase and squash interim commits**

Squash all task commits into a single meaningful commit on the feature branch:

```bash
git rebase -i main
```

Squash all commits into one with message:

```
feat(website): add SEO optimization and LLM discoverability

- Add missing assets: favicon.ico, og-social-card.jpg, robots.txt
- Configure sitemap, GA4 analytics (placeholder), Search Console (placeholder)
- Add JSON-LD structured data (SoftwareApplication + TechArticle)
- Add unique meta descriptions to all 62 documentation pages
- Create llms.txt for AI assistant discoverability
- Add build script to generate llms-full.txt from documentation
- Audit and fix generic link anchor text
```

**Note:** `git rebase -i` requires interactive input. Use `git rebase` with `--autosquash` or manually fixup. The implementer should handle this based on their preferred rebase workflow.
