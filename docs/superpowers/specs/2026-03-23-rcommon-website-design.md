# RCommon Documentation Website — Design Spec

**Date:** 2026-03-23
**Status:** Approved
**Branch:** feature/website-v2

## Overview

Build a comprehensive Docusaurus 3 documentation website for RCommon, hosted on GitHub Pages at `rcommon.com`. The site includes a marketing landing page and complete documentation for all 37+ NuGet packages. The design is adapted from Hasura's [ddn-docs](https://github.com/hasura/ddn-docs) open-source Docusaurus site.

## Goals

- Replace the existing docs.rcommon.com with a modern, comprehensive documentation site
- Provide complete documentation for every RCommon package, organized by domain
- Create a marketing landing page that communicates RCommon's value proposition
- Support versioned documentation from day one
- Deploy automatically via GitHub Actions to GitHub Pages

## Non-Goals

- AI chatbot integration (can be added later)
- Blog (Docusaurus supports it, but not in initial scope — structure will be there)
- Auto-generated API reference from XML doc comments (manual API summaries instead)

## Site Architecture

### Approach

Fork Hasura's [ddn-docs](https://github.com/hasura/ddn-docs) repository. Strip all Hasura content and Hasura-specific components. Adapt the Docusaurus 3 + TypeScript + Tailwind CSS infrastructure and reusable components for RCommon.

### Repository Layout

```
RCommon/
├── Src/                         # existing .NET source
├── Tests/                       # existing tests
├── Examples/                    # existing examples
├── website/                     # Docusaurus site
│   ├── docs/                    # documentation content (MDX)
│   │   ├── getting-started/
│   │   ├── core-concepts/
│   │   ├── domain-driven-design/
│   │   ├── persistence/
│   │   ├── cqrs-mediator/
│   │   ├── event-handling/
│   │   ├── messaging/
│   │   ├── caching/
│   │   ├── blob-storage/
│   │   ├── serialization/
│   │   ├── validation/
│   │   ├── email/
│   │   ├── multi-tenancy/
│   │   ├── security-web/
│   │   ├── architecture-guides/
│   │   ├── examples-recipes/
│   │   ├── testing/
│   │   └── api-reference/
│   ├── src/
│   │   ├── components/          # custom React components
│   │   ├── css/                 # Tailwind + custom styles
│   │   └── pages/               # landing page + custom pages
│   ├── static/                  # images, logos, CNAME
│   ├── docusaurus.config.ts
│   ├── sidebars.ts
│   ├── tailwind.config.js
│   └── package.json
├── .github/
│   └── workflows/
│       ├── build-dotnet8.yml    # existing — unchanged
│       └── deploy-website.yml   # new — GitHub Pages deployment
└── ...
```

### What We Keep from Hasura

- Docusaurus 3 + TypeScript configuration foundation
- Tailwind CSS setup with PostCSS
- Dark/light mode toggle infrastructure
- Sidebar auto-generation from filesystem
- Algolia search integration structure
- Mermaid diagram support
- Code syntax highlighting (GitHub light / Dracula dark)

### What We Strip

- All Hasura content (entire `docs/` folder)
- Hasura-specific components: `GraphiQLIDE`, `ConnectorGallery`, `DatabaseDocs`, `HasuraBanner`, `AiChatBot`, `CopyLlmText`, `LatestRelease`
- Hasura branding, logos, colors, favicons
- OpenReplay analytics
- Hasura version configurations
- Hasura-specific plugins and API integrations

### What We Adapt

- `docusaurus.config.ts` — RCommon branding, metadata, URLs, Algolia keys
- `sidebars.ts` — RCommon documentation hierarchy
- Theme colors — RCommon brand palette (provided by user)
- Footer — RCommon links (GitHub, NuGet, social)
- Navbar — RCommon logo, docs, API Reference, Examples, Blog, GitHub star badge

## Content Organization

Documentation is organized by **domain**, not by package name. Users think in terms of "persistence" or "events", not "RCommon.EfCore".

### Sidebar Structure

```
Getting Started
├── Overview & Philosophy
├── Installation
├── Quick Start Guide
├── Configuration & Bootstrapping
└── Dependency Injection

Core Concepts
├── Fluent Configuration Builder
├── Guards & Validation
├── GUID Generation
├── System Time Abstraction
└── Execution Results & Models

Domain-Driven Design
├── Entities & Aggregate Roots
├── Domain Events
├── Value Objects
├── Auditing (Created, Updated)
└── Soft Delete

Persistence
├── Repository Pattern
├── Specifications
├── Unit of Work
├── Providers
│   ├── Entity Framework Core
│   ├── Dapper
│   └── Linq2Db
└── Persistence Caching
    ├── Memory Cache
    └── Redis Cache

CQRS & Mediator
├── Command & Query Bus (RCommon.ApplicationServices)
├── Commands & Handlers
├── Queries & Handlers
└── Providers
    ├── MediatR
    └── Wolverine

Event Handling
├── Event Bus Overview
├── In-Memory Events
├── Distributed Events
├── Transactional Outbox
└── Providers
    ├── MediatR
    ├── MassTransit
    └── Wolverine

Messaging
├── Message Bus Overview
├── Transactional Outbox
├── State Machines (MassTransit)
└── Providers
    ├── MassTransit
    └── Wolverine

State Machines
├── State Machine Overview (RCommon.Stateless)
└── Stateless Integration

Caching
├── Caching Overview
├── Memory Cache
└── Redis Cache

Blob Storage
├── Blob Storage Overview
├── Azure Blob Storage
└── Amazon S3

Serialization
├── JSON Abstraction
├── Newtonsoft.Json
└── System.Text.Json

Validation
└── FluentValidation Integration

Email
├── Email Abstraction (RCommon.Emailing — includes built-in SMTP support)
└── SendGrid Provider (RCommon.SendGrid)

Multi-Tenancy
├── Multi-Tenancy Overview
└── Finbuckle Integration

Security & Web
├── Authorization
└── Web Utilities

Architecture Guides
├── Clean Architecture
├── Microservices
└── Event-Driven Architecture

Examples & Recipes
├── HR Leave Management (Clean + CQRS)
├── Event Handling Examples
├── Caching Examples
└── Messaging Examples

Testing
├── Testing with RCommon
└── Test Base Classes

API Reference
├── NuGet Packages
├── Changelog
└── Migration Guide
```

### Page Template

Every documentation page follows this structure:

1. **Overview** — What this feature is and when to use it
2. **Installation** — NuGet package(s) needed (using `NuGetInstall` component)
3. **Configuration** — Fluent builder setup in `Program.cs`
4. **Usage** — Code examples showing common patterns
5. **Provider tabs** — Side-by-side provider implementations where applicable
6. **Advanced usage** — Edge cases, customization, extension points
7. **API summary** — Key interfaces and classes with brief descriptions

## Landing Page

A marketing-style home page at `rcommon.com` built as a custom Docusaurus page.

### Sections (top to bottom)

1. **Navbar** — Logo, Docs, API Reference, Examples, Blog, Search bar, Version dropdown, dark/light toggle, GitHub star badge
2. **Hero** — Tagline ("Build Enterprise Applications Without Reinventing the Wheel"), description, dual CTA buttons (Get Started, View on GitHub), `dotnet add package` command
3. **Stats bar** — 37+ NuGet Packages, 3 Target Frameworks, 20+ Working Examples, Apache 2.0 License
4. **Feature cards** — 3×3 grid: Persistence, CQRS & Mediator, Event Handling, Messaging, Caching, DDD, Blob Storage, Multi-Tenancy, Validation & More
5. **Code example** — Fluent builder configuration showing the developer experience
6. **Architecture section** — Clean Architecture, Microservices, Event-Driven cards
7. **Footer** — Copyright, GitHub, NuGet, Twitter links

## Components

### Kept & Adapted from Hasura

| Component | RCommon Use |
|-----------|-------------|
| `OverviewIconCard` | Feature overview cards with icons on docs section landing pages |
| `OverviewPlainCard` | Listing sub-topics within a section |
| `OverviewTopSection` | Section intro with optional diagram |
| `CodeStep` | Step-by-step installation/configuration walkthroughs |
| `Feedback` | "Was this helpful?" on every documentation page |
| `Tabs` (MDX) | Provider tabs — show EF Core vs Dapper vs Linq2Db side-by-side |

### Removed (Hasura-specific)

`GraphiQLIDE`, `ConnectorGallery`, `DatabaseDocs`, `AiChatBot`, `HasuraBanner`, `CopyLlmText`, `LatestRelease`

### New Components

| Component | Purpose |
|-----------|---------|
| `NuGetInstall` | Styled install command block with copy button (`dotnet add package RCommon.Core`) |
| `ProviderComparison` | Table comparing providers for a given abstraction (e.g., EF Core vs Dapper feature matrix) |

## Deployment & Infrastructure

### GitHub Actions (`deploy-website.yml`)

- **Trigger:** Push to `main` when files in `website/` change + manual `workflow_dispatch`
- **Steps:** Checkout → Install pnpm → Install dependencies → Build Docusaurus → Deploy to GitHub Pages
- **Action:** `actions/deploy-pages`

### GitHub Pages

- **Source:** GitHub Actions (not branch-based)
- **Custom domain:** `rcommon.com` via `CNAME` file in `website/static/`
- **HTTPS:** Enforced

### Algolia DocSearch

- Apply to the [DocSearch free program](https://docsearch.algolia.com/apply/) (qualifies as open-source)
- Config in `docusaurus.config.ts` under `themeConfig.algolia`
- Launch with local search plugin (`@easyops-cn/docusaurus-search-local`) as fallback until Algolia approval

### Versioning

- Docusaurus built-in versioning via `docusaurus docs:version` command
- Current docs are the "next" (unreleased) version during development
- Snapshot a version (e.g., `1.0`) when ready for launch
- Version dropdown in navbar

### Build Integration

- Existing `build-dotnet8.yml` stays untouched
- Website deployment is a separate, independent workflow
- No coupling between .NET builds and docs builds

## Content Strategy

### Content Sources (priority order)

1. **Source code** — Interfaces, classes, XML doc comments, method signatures from `Src/`
2. **Package READMEs** — Each NuGet package has an embedded README with usage examples
3. **Examples folder** — 20 working example projects demonstrating real patterns
4. **Test projects** — 38 test projects showing expected behavior and edge cases
5. **Existing docs** — Current docs.rcommon.com content for covered topics
6. **Design specs** — Recent specs in `docs/superpowers/specs/` for new features

### Content Generation Approach

- Build the Docusaurus site structure and components first (the shell)
- Generate documentation content working through the sidebar top-to-bottom
- Use source code as the authority — every code example verified against the actual API
- Pull real examples from `Examples/` rather than inventing synthetic ones

### Estimated Volume

- ~15 top-level sections
- ~60-70 individual documentation pages
- Each page: 500-1500 words with 2-5 code examples

## Dependencies

- **User-provided:** RCommon logo (SVG), favicon/icon, brand color palette
- **External:** Algolia DocSearch application approval (use local search as fallback)
- **Infrastructure:** GitHub Pages enabled on the repository, custom domain DNS configured
