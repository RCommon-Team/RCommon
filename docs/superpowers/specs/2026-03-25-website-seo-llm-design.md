# Website SEO & LLM Optimization — Design Spec

**Date:** 2026-03-25
**Branch:** `feature/read-me-website-seo`
**Related spec:** `2026-03-23-rcommon-website-design.md` (Phase 1 complete)

## Context

The RCommon documentation website (Docusaurus 3, deployed to GitHub Pages at `rcommon.com`) completed Phase 1 — site shell, components, full docs structure with ~62 MDX pages, and CI/CD. However, SEO fundamentals and LLM discoverability were deferred. This spec covers the full SEO overhaul and LLM-friendly content additions.

## Goals

1. **Traditional SEO:** Fix missing assets, add per-page meta descriptions, implement structured data, enable analytics and search console integration.
2. **LLM discoverability:** Make RCommon discoverable when developers ask AI assistants about .NET infrastructure libraries.
3. **LLM consumption:** Make documentation content easy for AI tools to ingest, chunk, and cite.

## Non-Goals

- Algolia DocSearch integration (planned for post-launch, per original website spec)
- Content writing or rewriting (meta additions only, not doc content changes)
- Performance optimization (Docusaurus defaults are sufficient)
- Cookie consent banner (can be added later for compliance if needed)
- Blog or social media strategy

---

## 1. SEO Foundation — Missing Assets & Crawl Basics

### Problem

Several assets referenced in `docusaurus.config.ts` do not exist, and basic crawl directives are missing.

### Deliverables

| Item | Location | Details |
|------|----------|---------|
| `favicon.ico` | `static/img/favicon.ico` | Generated from existing `rcommon-logo.png`. Multi-size ICO (16x16, 32x32, 48x48). |
| `og-social-card.jpg` | `static/img/og-social-card.jpg` | 1200x630 image with RCommon logo, project name, and tagline on branded background. Used for Open Graph and Twitter Card meta tags. |
| `robots.txt` | `static/robots.txt` | Allow all crawlers, reference sitemap URL. |
| Sitemap config | `docusaurus.config.ts` | Explicitly configure `@docusaurus/plugin-sitemap` in the classic preset with `changefreq` and `priority` settings. |

### robots.txt content

```
User-agent: *
Allow: /

Sitemap: https://rcommon.com/sitemap.xml
```

### Acceptance criteria

- `favicon.ico` renders in browser tabs when visiting any page.
- Sharing a page URL on Slack/Twitter/LinkedIn shows the OG image with title and description.
- `https://rcommon.com/robots.txt` returns valid directives.
- `https://rcommon.com/sitemap.xml` returns a valid sitemap with all doc pages.

---

## 2. Per-Page Meta Descriptions

### Problem

All 62+ MDX pages only have `title` and `sidebar_position` in frontmatter. Docusaurus falls back to the global tagline for `<meta name="description">` on every page, making all pages appear identical in search results.

### Approach

Add a `description` field to every MDX file's YAML frontmatter. Docusaurus automatically maps this to `<meta name="description">` and `og:description`.

### Requirements

- Each description must be 150-160 characters.
- Each description must be unique across all pages.
- Descriptions should include relevant keywords naturally (e.g., "EF Core repository", "MediatR CQRS", "transactional outbox").
- Descriptions should describe what the specific page covers, not the overall project.

### Example

**Before:**
```yaml
---
title: Entity Framework Core
sidebar_position: 4
---
```

**After:**
```yaml
---
title: Entity Framework Core
sidebar_position: 4
description: "Configure EF Core as your RCommon persistence provider with repository pattern, LINQ queries, eager loading, and multi-DbContext support."
---
```

### Acceptance criteria

- Every MDX file in `website/docs/` has a unique `description` in frontmatter.
- No description exceeds 160 characters.
- `view-source` on any doc page shows the description in both `<meta name="description">` and `<meta property="og:description">`.

---

## 3. Structured Data (JSON-LD)

### Problem

No schema.org structured data exists. Search engines cannot generate rich results for RCommon pages.

### Approach

A custom Docusaurus plugin (`plugins/seo-structured-data.ts`) that uses the `injectHtmlTags` lifecycle hook to add JSON-LD `<script>` tags based on route.

### Homepage — SoftwareApplication

```json
{
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  "name": "RCommon",
  "description": "Open-source .NET infrastructure library providing battle-tested abstractions for persistence, CQRS, event handling, messaging, caching, and more.",
  "url": "https://rcommon.com",
  "applicationCategory": "DeveloperApplication",
  "operatingSystem": ".NET 8, .NET 9, .NET 10",
  "license": "https://opensource.org/licenses/Apache-2.0",
  "offers": {
    "@type": "Offer",
    "price": "0",
    "priceCurrency": "USD"
  },
  "sourceOrganization": {
    "@type": "Organization",
    "name": "RCommon Team",
    "url": "https://github.com/RCommon-Team"
  }
}
```

### Doc Pages — TechArticle

```json
{
  "@context": "https://schema.org",
  "@type": "TechArticle",
  "headline": "<page title from frontmatter>",
  "description": "<page description from frontmatter>",
  "url": "<canonical URL>",
  "publisher": {
    "@type": "Organization",
    "name": "RCommon Team"
  },
  "about": {
    "@type": "SoftwareApplication",
    "name": "RCommon"
  }
}
```

### Implementation

The plugin reads route metadata at build time. For the homepage (`/`), it injects `SoftwareApplication`. For doc routes (`/docs/**`), it injects `TechArticle` with the page's title and description pulled from frontmatter.

### Acceptance criteria

- Google Rich Results Test validates the homepage JSON-LD as `SoftwareApplication`.
- Google Rich Results Test validates any doc page JSON-LD as `TechArticle`.
- No JSON-LD validation errors or warnings.

---

## 4. Google Analytics 4

### Problem

No analytics — impossible to measure SEO impact or understand traffic.

### Approach

Use the built-in `gtag` plugin from `@docusaurus/preset-classic`. No additional packages required.

### Config change

```typescript
// in docusaurus.config.ts preset options
gtag: {
  trackingID: 'G-XXXXXXXXXX', // placeholder — swap when GA4 property is created
  anonymizeIP: true,
},
```

### Google Search Console

Add a verification meta tag placeholder via `headTags` in `docusaurus.config.ts`:

```typescript
headTags: [
  {
    tagName: 'meta',
    attributes: {
      name: 'google-site-verification',
      content: 'YOUR_VERIFICATION_CODE',
    },
  },
],
```

### Acceptance criteria

- GA4 script loads on all pages (verifiable in browser DevTools Network tab once the real tracking ID is set).
- `anonymizeIP` is enabled.
- Search Console verification meta tag is present in page source.
- Both IDs are clearly marked as placeholders in config with comments explaining how to replace them.

---

## 5. LLM-Friendly Content

### 5a. llms.txt (handcrafted)

**Location:** `static/llms.txt` → served at `https://rcommon.com/llms.txt`

A curated, human-written summary following the [llms.txt convention](https://llmstxt.org/). Contains:

- Project name and one-paragraph description
- Key facts (license, target frameworks, package count)
- Core abstractions with brief descriptions and links to relevant doc sections
- Links to getting started, full documentation, GitHub, and NuGet
- Link to `llms-full.txt` for comprehensive content

This file is maintained manually. Updates are needed only when major features are added or removed.

### 5b. llms-full.txt (build-time generated)

**Location:** `static/llms-full.txt` → served at `https://rcommon.com/llms-full.txt`

A comprehensive single-file document containing all documentation content, optimized for LLM/RAG consumption.

**Generator:** `scripts/generate-llms-full.ts`

The script:
1. Reads all MDX files from `website/docs/` following sidebar ordering.
2. Strips MDX-specific syntax: import statements, JSX component tags (e.g., `<NuGetInstall>`, `<ProviderComparison>`).
3. Preserves all markdown structure: headings, code blocks, lists, tables.
4. Adds section headers with source URLs (e.g., `## Entity Framework Core\nSource: https://rcommon.com/docs/persistence/efcore`).
5. Outputs to `static/llms-full.txt`.

**Build integration:**
```json
{
  "scripts": {
    "generate:llms": "tsx scripts/generate-llms-full.ts",
    "build": "pnpm generate:llms && docusaurus build"
  }
}
```

### 5c. Semantic link audit

Review all internal links across docs for descriptive anchor text. Replace any generic text ("click here", "see this", "link") with descriptive alternatives that tell both humans and LLMs what the target page covers.

### Acceptance criteria

- `https://rcommon.com/llms.txt` returns a well-structured markdown summary.
- `https://rcommon.com/llms-full.txt` contains the full documentation content without JSX artifacts.
- `llms-full.txt` is regenerated on every build (not stale).
- No internal links use generic anchor text.

---

## File Inventory

### New files

| File | Purpose |
|------|---------|
| `website/static/robots.txt` | Crawl directives |
| `website/static/img/favicon.ico` | Browser tab icon |
| `website/static/img/og-social-card.jpg` | Social sharing image |
| `website/static/llms.txt` | LLM discovery file (handcrafted) |
| `website/plugins/seo-structured-data.ts` | JSON-LD injection plugin |
| `website/scripts/generate-llms-full.ts` | Build script for llms-full.txt |

### Modified files

| File | Changes |
|------|---------|
| `website/docusaurus.config.ts` | Add gtag, sitemap config, headTags, plugin registration |
| `website/package.json` | Add `generate:llms` and `tsx` dev dependency, update `build` script |
| `website/src/pages/index.tsx` | More SEO-targeted description in Layout component |
| All 62 MDX files in `website/docs/` | Add `description` frontmatter |

---

## Dependencies

| Package | Purpose | Type |
|---------|---------|------|
| `tsx` | Run TypeScript build scripts (for `generate-llms-full.ts`) | devDependency |

No other new packages required. GA4 gtag, sitemap, and structured data all use built-in Docusaurus capabilities or custom plugins with no external dependencies.
