# RCommon Documentation Website Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a comprehensive Docusaurus 3 documentation website for RCommon at rcommon.com with a marketing landing page and complete documentation for all 37+ packages.

**Architecture:** Fork Hasura's ddn-docs into `website/`, strip Hasura-specific code, adapt components and theming for RCommon, build a marketing landing page, generate comprehensive documentation organized by domain, and deploy via GitHub Actions to GitHub Pages.

**Tech Stack:** Docusaurus 3, React 18, TypeScript, Tailwind CSS 3, pnpm, MDX, GitHub Actions, GitHub Pages

**Spec:** `docs/superpowers/specs/2026-03-23-rcommon-website-design.md`

---

## File Structure Overview

### New files/directories created

```
website/                              # Docusaurus project root (adapted from Hasura ddn-docs)
├── docs/                             # All documentation content (MDX)
│   ├── getting-started/              # 5 pages
│   ├── core-concepts/                # 5 pages
│   ├── domain-driven-design/         # 5 pages
│   ├── persistence/                  # 8 pages
│   ├── cqrs-mediator/                # 5 pages
│   ├── event-handling/               # 6 pages
│   ├── messaging/                    # 5 pages
│   ├── state-machines/               # 2 pages
│   ├── caching/                      # 3 pages
│   ├── blob-storage/                 # 3 pages
│   ├── serialization/                # 3 pages
│   ├── validation/                   # 1 page
│   ├── email/                        # 2 pages
│   ├── multi-tenancy/                # 2 pages
│   ├── security-web/                 # 2 pages
│   ├── architecture-guides/          # 3 pages
│   ├── examples-recipes/             # 4 pages
│   ├── testing/                      # 2 pages
│   ├── api-reference/                # 3 pages
│   └── index.mdx                     # Docs landing page
├── src/
│   ├── components/
│   │   ├── NuGetInstall/             # NEW — styled install command block
│   │   │   ├── index.tsx
│   │   │   └── styles.module.css
│   │   ├── ProviderComparison/       # NEW — provider feature matrix table
│   │   │   ├── index.tsx
│   │   │   └── styles.module.css
│   │   ├── OverviewIconCard/         # ADAPTED from Hasura
│   │   ├── OverviewPlainCard/        # ADAPTED from Hasura
│   │   ├── OverviewTopSection/       # ADAPTED from Hasura
│   │   ├── CodeStep/                 # KEPT from Hasura
│   │   ├── Feedback/                 # KEPT from Hasura
│   │   └── CustomFooter/             # ADAPTED from Hasura
│   ├── css/
│   │   └── custom.css                # ADAPTED — RCommon colors/branding
│   ├── pages/
│   │   ├── index.tsx                 # NEW — marketing landing page
│   │   └── index.module.css          # NEW — landing page styles
│   └── theme/                        # KEPT — Docusaurus theme overrides
├── static/
│   ├── img/
│   │   ├── logo.svg                  # USER-PROVIDED — RCommon logo
│   │   ├── logo-dark.svg             # USER-PROVIDED — dark mode logo
│   │   ├── favicon.ico               # USER-PROVIDED — favicon
│   │   └── og-social-card.jpg        # NEW — Open Graph card
│   ├── icons/                        # NEW — section/feature icons
│   └── CNAME                         # NEW — contains "rcommon.com"
├── docusaurus.config.ts              # ADAPTED from Hasura
├── sidebars.ts                       # NEW — RCommon sidebar config
├── tailwind.config.js                # ADAPTED from Hasura
├── tsconfig.json                     # KEPT from Hasura
├── package.json                      # ADAPTED — pnpm, stripped deps
└── .gitignore                        # ADAPTED from Hasura
```

### Modified existing files

```
.github/workflows/deploy-website.yml  # NEW — GitHub Pages deployment
.gitignore                             # ADD .superpowers/ entry
```

---

## Phase 1: Site Shell

### Task 1: Clone Hasura ddn-docs into website/

**Files:**
- Create: `website/` (entire directory from Hasura clone)

- [ ] **Step 1: Clone the Hasura ddn-docs repository**

```bash
cd c:/Users/jason.webb/source/repos/RCommon
git clone https://github.com/hasura/ddn-docs.git website-temp
```

- [ ] **Step 2: Copy contents into website/ directory (excluding .git)**

```bash
mkdir website
rsync -a --exclude .git website-temp/ website/
rm -rf website-temp
```

- [ ] **Step 3: Verify the clone has the expected structure**

```bash
ls website/
```

Expected: `docusaurus.config.ts`, `package.json`, `sidebars.ts`, `tailwind.config.js`, `src/`, `docs/`, `static/`, etc.

- [ ] **Step 4: Commit the raw fork**

```bash
git add website/
git commit -m "chore: clone Hasura ddn-docs as website/ base"
```

---

### Task 2: Convert from Yarn to pnpm

**Files:**
- Modify: `website/package.json`
- Delete: `website/yarn.lock`, `website/.yarnrc.yml`, `website/.yarn/`

- [ ] **Step 1: Remove Yarn-specific files**

```bash
rm -rf website/.yarn website/.yarnrc.yml website/yarn.lock website/package-lock.json
```

- [ ] **Step 2: Update package.json — strip Hasura-specific dependencies**

Read `website/package.json` and update:

Remove these dependencies (Hasura-specific):
- `@openreplay/tracker`
- `graphiql`
- `graphql`
- `graphql-ws`
- `dompurify` (used by AiChatBot)
- `js-cookie` (used by OpenReplay)
- `markdown-to-jsx` (used by AiChatBot)
- `posthog-js`
- `react-transition-group`
- `uuid`

Remove these devDependencies:
- `@types/dompurify`
- `@types/js-cookie`
- `@types/punycode`
- `@types/react-transition-group`

Keep these dependencies:
- `@docusaurus/core`
- `@docusaurus/plugin-content-docs`
- `@docusaurus/preset-classic`
- `@docusaurus/theme-mermaid`
- `@mdx-js/react`
- `autoprefixer`
- `clsx`
- `postcss`
- `prism-react-renderer`
- `react`
- `react-dom`
- `tailwindcss`
- `usehooks-ts`

Add for local search fallback:
- `@easyops-cn/docusaurus-search-local`

Keep these devDependencies:
- `@docusaurus/module-type-aliases`
- `@docusaurus/tsconfig`
- `@docusaurus/types`
- `dotenv`
- `typescript`

- [ ] **Step 3: Install dependencies with pnpm**

```bash
cd website && pnpm install
```

- [ ] **Step 4: Verify pnpm install succeeded**

```bash
ls website/node_modules/@docusaurus/core
```

Expected: directory exists

- [ ] **Step 5: Commit**

```bash
git add website/package.json website/pnpm-lock.yaml
git rm --cached website/yarn.lock website/.yarnrc.yml 2>/dev/null; true
git add -u website/
git commit -m "chore: convert website from Yarn to pnpm, strip Hasura deps"
```

---

### Task 3: Strip Hasura content and components

**Files:**
- Delete: `website/docs/` (all Hasura content)
- Delete: `website/wiki/`
- Delete: `website/utilities/`
- Delete: `website/k8s-manifest/`
- Delete: `website/schema_examples/`
- Delete: `website/Dockerfile`, `website/Dockerfile.dev`, `website/docker-compose.yaml`
- Delete: `website/scraper.config.json`, `website/custom-build-script.cjs`
- Delete: `website/.dockerignore`, `website/.envrc`, `website/.kodiak.toml`
- Delete: `website/flake.lock`, `website/flake.nix`
- Delete: Hasura-specific components in `website/src/components/`
- Delete: Hasura static assets in `website/static/`

- [ ] **Step 1: Remove Hasura content directories**

```bash
rm -rf website/docs website/wiki website/utilities website/k8s-manifest website/schema_examples
```

- [ ] **Step 2: Remove Hasura infrastructure files**

```bash
rm -f website/Dockerfile website/Dockerfile.dev website/docker-compose.yaml
rm -f website/scraper.config.json website/custom-build-script.cjs
rm -f website/.dockerignore website/.envrc website/.kodiak.toml
rm -f website/flake.lock website/flake.nix
```

- [ ] **Step 3: Remove Hasura-specific components**

```bash
rm -rf website/src/components/AiChatBot
rm -rf website/src/components/GraphiQLIDE
rm -rf website/src/components/ConnectorGallery
rm -rf website/src/components/databaseDocs
rm -rf website/src/components/HasuraBanner
rm -rf website/src/components/CopyLlmText
rm -rf website/src/components/LatestRelease
rm -rf website/src/components/CliVersion
rm -rf website/src/components/CanonicalUrl
rm -rf website/src/components/OpenReplay
rm -rf website/src/components/HomepageFeatures
rm -rf website/src/components/SimpleVideo
```

- [ ] **Step 4: Remove Hasura static assets**

```bash
rm -rf website/static/img/*
rm -rf website/static/icons/*
```

Keep the `website/static/.nojekyll` file (needed for GitHub Pages).

- [ ] **Step 5: Create placeholder docs directory**

```bash
mkdir -p website/docs
```

Create `website/docs/index.mdx`:

```mdx
---
title: RCommon Documentation
sidebar_position: 1
---

# RCommon Documentation

Welcome to the RCommon documentation. Use the sidebar to navigate through the available topics.
```

- [ ] **Step 6: Commit**

```bash
git add -A website/
git commit -m "chore: strip all Hasura content, components, and assets"
```

---

### Task 4: Configure docusaurus.config.ts for RCommon

**Files:**
- Modify: `website/docusaurus.config.ts`

- [ ] **Step 1: Read the current Hasura config**

Read `website/docusaurus.config.ts` to understand its structure.

- [ ] **Step 2: Rewrite docusaurus.config.ts for RCommon**

Replace the entire file. Key configuration:

```typescript
import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'RCommon',
  tagline: 'Build Enterprise Applications Without Reinventing the Wheel',
  favicon: 'img/favicon.ico',
  url: 'https://rcommon.com',
  baseUrl: '/',
  organizationName: 'RCommon-Team',
  projectName: 'RCommon',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  trailingSlash: false,

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/RCommon-Team/RCommon/tree/main/website/',
        },
        blog: false, // disabled for initial launch
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themes: ['@docusaurus/theme-mermaid'],

  markdown: {
    mermaid: true,
  },

  themeConfig: {
    image: 'img/og-social-card.jpg',
    navbar: {
      title: 'RCommon',
      logo: {
        alt: 'RCommon Logo',
        src: 'img/logo.svg',
        srcDark: 'img/logo-dark.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'docsVersionDropdown',
          position: 'right',
        },
        {
          href: 'https://github.com/RCommon-Team/RCommon',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://www.nuget.org/profiles/RCommon',
          label: 'NuGet',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            { label: 'Getting Started', to: '/docs/getting-started' },
            { label: 'Persistence', to: '/docs/persistence' },
            { label: 'CQRS & Mediator', to: '/docs/cqrs-mediator' },
          ],
        },
        {
          title: 'Community',
          items: [
            { label: 'GitHub', href: 'https://github.com/RCommon-Team/RCommon' },
            { label: 'NuGet', href: 'https://www.nuget.org/profiles/RCommon' },
          ],
        },
        {
          title: 'More',
          items: [
            { label: 'License', href: 'https://github.com/RCommon-Team/RCommon/blob/main/LICENSE' },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} RCommon Team. Apache 2.0 License.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'json', 'bash', 'xml'],
    },
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
  } satisfies Preset.ThemeConfig,

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
  ],
};

export default config;
```

- [ ] **Step 3: Verify config syntax**

```bash
cd website && npx tsc --noEmit docusaurus.config.ts 2>&1 || echo "Will verify on build"
```

- [ ] **Step 4: Commit**

```bash
git add website/docusaurus.config.ts
git commit -m "feat: configure docusaurus.config.ts for RCommon"
```

---

### Task 5: Configure sidebars.ts

**Files:**
- Modify: `website/sidebars.ts`

- [ ] **Step 1: Replace sidebars.ts with RCommon sidebar config**

```typescript
import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    {
      type: 'category',
      label: 'Getting Started',
      link: { type: 'generated-index' },
      items: [
        'getting-started/overview',
        'getting-started/installation',
        'getting-started/quick-start',
        'getting-started/configuration',
        'getting-started/dependency-injection',
      ],
    },
    {
      type: 'category',
      label: 'Core Concepts',
      link: { type: 'generated-index' },
      items: [
        'core-concepts/fluent-configuration',
        'core-concepts/guards',
        'core-concepts/guid-generation',
        'core-concepts/system-time',
        'core-concepts/execution-results',
      ],
    },
    {
      type: 'category',
      label: 'Domain-Driven Design',
      link: { type: 'generated-index' },
      items: [
        'domain-driven-design/entities-aggregates',
        'domain-driven-design/domain-events',
        'domain-driven-design/value-objects',
        'domain-driven-design/auditing',
        'domain-driven-design/soft-delete',
      ],
    },
    {
      type: 'category',
      label: 'Persistence',
      link: { type: 'generated-index' },
      items: [
        'persistence/repository-pattern',
        'persistence/specifications',
        'persistence/unit-of-work',
        {
          type: 'category',
          label: 'Providers',
          items: [
            'persistence/efcore',
            'persistence/dapper',
            'persistence/linq2db',
          ],
        },
        {
          type: 'category',
          label: 'Persistence Caching',
          items: [
            'persistence/caching-memory',
            'persistence/caching-redis',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'CQRS & Mediator',
      link: { type: 'generated-index' },
      items: [
        'cqrs-mediator/command-query-bus',
        'cqrs-mediator/commands-handlers',
        'cqrs-mediator/queries-handlers',
        {
          type: 'category',
          label: 'Providers',
          items: [
            'cqrs-mediator/mediatr',
            'cqrs-mediator/wolverine',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'Event Handling',
      link: { type: 'generated-index' },
      items: [
        'event-handling/overview',
        'event-handling/in-memory',
        'event-handling/distributed',
        'event-handling/transactional-outbox',
        {
          type: 'category',
          label: 'Providers',
          items: [
            'event-handling/mediatr',
            'event-handling/masstransit',
            'event-handling/wolverine',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'Messaging',
      link: { type: 'generated-index' },
      items: [
        'messaging/overview',
        'messaging/transactional-outbox',
        'messaging/state-machines',
        {
          type: 'category',
          label: 'Providers',
          items: [
            'messaging/masstransit',
            'messaging/wolverine',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'State Machines',
      link: { type: 'generated-index' },
      items: [
        'state-machines/overview',
        'state-machines/stateless',
      ],
    },
    {
      type: 'category',
      label: 'Caching',
      link: { type: 'generated-index' },
      items: [
        'caching/overview',
        'caching/memory',
        'caching/redis',
      ],
    },
    {
      type: 'category',
      label: 'Blob Storage',
      link: { type: 'generated-index' },
      items: [
        'blob-storage/overview',
        'blob-storage/azure',
        'blob-storage/s3',
      ],
    },
    {
      type: 'category',
      label: 'Serialization',
      link: { type: 'generated-index' },
      items: [
        'serialization/overview',
        'serialization/newtonsoft',
        'serialization/system-text-json',
      ],
    },
    {
      type: 'category',
      label: 'Validation',
      link: { type: 'generated-index' },
      items: [
        'validation/fluent-validation',
      ],
    },
    {
      type: 'category',
      label: 'Email',
      link: { type: 'generated-index' },
      items: [
        'email/overview',
        'email/sendgrid',
      ],
    },
    {
      type: 'category',
      label: 'Multi-Tenancy',
      link: { type: 'generated-index' },
      items: [
        'multi-tenancy/overview',
        'multi-tenancy/finbuckle',
      ],
    },
    {
      type: 'category',
      label: 'Security & Web',
      link: { type: 'generated-index' },
      items: [
        'security-web/authorization',
        'security-web/web-utilities',
      ],
    },
    {
      type: 'category',
      label: 'Architecture Guides',
      link: { type: 'generated-index' },
      items: [
        'architecture-guides/clean-architecture',
        'architecture-guides/microservices',
        'architecture-guides/event-driven',
      ],
    },
    {
      type: 'category',
      label: 'Examples & Recipes',
      link: { type: 'generated-index' },
      items: [
        'examples-recipes/hr-leave-management',
        'examples-recipes/event-handling',
        'examples-recipes/caching',
        'examples-recipes/messaging',
      ],
    },
    {
      type: 'category',
      label: 'Testing',
      link: { type: 'generated-index' },
      items: [
        'testing/overview',
        'testing/test-base-classes',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      link: { type: 'generated-index' },
      items: [
        'api-reference/nuget-packages',
        'api-reference/changelog',
        'api-reference/migration-guide',
      ],
    },
  ],
};

export default sidebars;
```

- [ ] **Step 2: Commit**

```bash
git add website/sidebars.ts
git commit -m "feat: configure RCommon sidebar navigation structure"
```

---

### Task 6: Set up Tailwind theme and branding

**Files:**
- Modify: `website/tailwind.config.js`
- Modify: `website/src/css/custom.css`
- Create: `website/static/CNAME`
- Create: `website/static/img/logo.svg` (placeholder until user provides)
- Create: `website/static/img/logo-dark.svg` (placeholder until user provides)
- Create: `website/static/img/favicon.ico` (placeholder until user provides)

- [ ] **Step 1: Read Hasura's custom.css to understand the structure**

Read `website/src/css/custom.css`.

- [ ] **Step 2: Replace custom.css with RCommon-branded styles**

Keep the Tailwind directives and Docusaurus CSS variable structure. Replace Hasura colors with a professional blue/dark theme:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

:root {
  /* Primary brand colors */
  --ifm-color-primary: #2563eb;
  --ifm-color-primary-dark: #1d4ed8;
  --ifm-color-primary-darker: #1e40af;
  --ifm-color-primary-darkest: #1e3a8a;
  --ifm-color-primary-light: #3b82f6;
  --ifm-color-primary-lighter: #60a5fa;
  --ifm-color-primary-lightest: #93bbfd;

  /* Layout */
  --ifm-code-font-size: 95%;
  --docusaurus-highlighted-code-line-bg: rgba(0, 0, 0, 0.1);

  /* Navbar */
  --ifm-navbar-background-color: #ffffff;
  --ifm-navbar-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);

  /* Footer */
  --ifm-footer-background-color: #1e293b;
  --ifm-footer-color: #e2e8f0;
}

[data-theme='dark'] {
  --ifm-color-primary: #3b82f6;
  --ifm-color-primary-dark: #2563eb;
  --ifm-color-primary-darker: #1d4ed8;
  --ifm-color-primary-darkest: #1e40af;
  --ifm-color-primary-light: #60a5fa;
  --ifm-color-primary-lighter: #93bbfd;
  --ifm-color-primary-lightest: #bfdbfe;

  --ifm-background-color: #0f172a;
  --ifm-background-surface-color: #1e293b;
  --docusaurus-highlighted-code-line-bg: rgba(0, 0, 0, 0.3);

  --ifm-navbar-background-color: #1e293b;
  --ifm-footer-background-color: #0f172a;
}

/* Sidebar */
.theme-doc-sidebar-container {
  border-right: 1px solid var(--ifm-toc-border-color);
}

/* Code blocks */
.prism-code {
  border-radius: 8px;
}

/* Tables */
table {
  display: table;
  width: 100%;
}

th {
  background-color: var(--ifm-background-surface-color);
}

/* Admonitions */
.admonition {
  border-radius: 8px;
}
```

Note: This is a starting point. The user will provide brand colors to replace the blue palette. Keep any Hasura structural CSS patterns (responsive breakpoints, sidebar widths, etc.) that work well — just replace colors and branding.

- [ ] **Step 3: Update tailwind.config.js**

```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  corePlugins: {
    preflight: false,
  },
  content: ['./src/**/*.{js,jsx,ts,tsx}', './docs/**/*.mdx'],
  darkMode: ['class', '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93bbfd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
        },
      },
    },
  },
  plugins: [],
};
```

- [ ] **Step 4: Create CNAME file**

Create `website/static/CNAME`:
```
rcommon.com
```

- [ ] **Step 5: Create placeholder logo files**

Create simple SVG placeholder logos. These will be replaced by the user:

`website/static/img/logo.svg`:
```svg
<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
  <rect width="32" height="32" rx="6" fill="#2563eb"/>
  <text x="16" y="22" font-family="Arial" font-size="18" font-weight="bold" fill="white" text-anchor="middle">R</text>
</svg>
```

`website/static/img/logo-dark.svg`:
```svg
<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
  <rect width="32" height="32" rx="6" fill="#3b82f6"/>
  <text x="16" y="22" font-family="Arial" font-size="18" font-weight="bold" fill="white" text-anchor="middle">R</text>
</svg>
```

- [ ] **Step 6: Commit**

```bash
git add website/src/css/custom.css website/tailwind.config.js website/static/
git commit -m "feat: set up RCommon theme, branding, and CNAME"
```

---

### Task 7: Adapt kept components from Hasura

**Files:**
- Modify: `website/src/components/OverviewIconCard/` — remove Hasura-specific icons/links
- Modify: `website/src/components/OverviewPlainCard/` — remove Hasura-specific content
- Modify: `website/src/components/OverviewTopSection/` — remove video dependency
- Modify: `website/src/components/CodeStep/` — keep as-is (generic)
- Modify: `website/src/components/Feedback/` — keep as-is (generic)
- Modify: `website/src/components/CustomFooter/` — update links for RCommon

- [ ] **Step 1: Read each kept component to understand its structure**

Read these files:
- `website/src/components/OverviewIconCard/index.tsx`
- `website/src/components/OverviewPlainCard/index.tsx`
- `website/src/components/OverviewTopSection/index.tsx`
- `website/src/components/CodeStep/index.tsx`
- `website/src/components/Feedback/index.tsx`
- `website/src/components/CustomFooter/index.tsx`

- [ ] **Step 2: Update OverviewIconCard**

Remove any Hasura-specific icon references or hardcoded paths. Ensure it accepts generic props: `title`, `description`, `icon`, `link`.

- [ ] **Step 3: Update OverviewTopSection**

Remove the video player dependency. Make the video/image optional. Keep the layout structure for section introductions.

- [ ] **Step 4: Update CustomFooter**

Replace all Hasura links with RCommon links:
- GitHub: https://github.com/RCommon-Team/RCommon
- NuGet: https://www.nuget.org/profiles/RCommon
- Copyright: RCommon Team

- [ ] **Step 5: Remove any remaining Hasura imports**

Search all kept component files for imports referencing deleted components (AiChatBot, GraphiQLIDE, etc.) and remove them.

```bash
cd website && grep -r "AiChatBot\|GraphiQLIDE\|ConnectorGallery\|HasuraBanner\|OpenReplay\|LatestRelease\|CopyLlmText\|CliVersion\|databaseDocs" src/
```

Fix any broken imports found.

- [ ] **Step 6: Commit**

```bash
git add website/src/components/
git commit -m "feat: adapt Hasura components for RCommon use"
```

---

### Task 8: Fix theme overrides

**Files:**
- Modify: files in `website/src/theme/`

- [ ] **Step 1: Read each theme override**

Read all files under `website/src/theme/` to identify Hasura-specific customizations:
- `Admonition/Icon/`
- `AnnouncementBar/`
- `CodeBlock/CopyButton/`
- `DocItem/`
- `DocPaginator/`
- `DocRoot/Layout/`
- `DocSidebarItem/`
- `Footer/`
- `Navbar/`
- `TOC/`

- [ ] **Step 2: Remove or fix Hasura-specific theme overrides**

For each file:
- Remove imports of deleted components (AiChatBot, HasuraBanner, OpenReplay, etc.)
- Remove Hasura-specific logic (GTM analytics, Hasura banner rendering)
- Keep structural improvements that are useful (sidebar customizations, TOC styling, etc.)
- If a theme override only existed for a Hasura-specific feature, delete the entire override file to fall back to Docusaurus defaults

- [ ] **Step 3: Search for remaining broken references**

```bash
cd website && grep -r "hasura\|Hasura\|HASURA\|graphiql\|ddn\|DDN" src/ --include="*.tsx" --include="*.ts" --include="*.css" -l
```

Fix all remaining Hasura references.

- [ ] **Step 4: Commit**

```bash
git add website/src/theme/
git commit -m "fix: clean Hasura references from theme overrides"
```

---

### Task 9: Build new components — NuGetInstall and ProviderComparison

**Files:**
- Create: `website/src/components/NuGetInstall/index.tsx`
- Create: `website/src/components/NuGetInstall/styles.module.css`
- Create: `website/src/components/ProviderComparison/index.tsx`
- Create: `website/src/components/ProviderComparison/styles.module.css`

- [ ] **Step 1: Create NuGetInstall component**

`website/src/components/NuGetInstall/index.tsx`:

```tsx
import React, {useState} from 'react';
import styles from './styles.module.css';

interface NuGetInstallProps {
  packageName: string;
  version?: string;
}

export default function NuGetInstall({packageName, version}: NuGetInstallProps): JSX.Element {
  const [copied, setCopied] = useState(false);
  const command = version
    ? `dotnet add package ${packageName} --version ${version}`
    : `dotnet add package ${packageName}`;

  const handleCopy = () => {
    navigator.clipboard.writeText(command);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className={styles.container}>
      <div className={styles.label}>NuGet Package</div>
      <div className={styles.commandRow}>
        <code className={styles.command}>{command}</code>
        <button className={styles.copyButton} onClick={handleCopy} title="Copy to clipboard">
          {copied ? '✓' : '📋'}
        </button>
      </div>
    </div>
  );
}
```

`website/src/components/NuGetInstall/styles.module.css`:

```css
.container {
  border: 1px solid var(--ifm-color-emphasis-300);
  border-radius: 8px;
  padding: 12px 16px;
  margin: 16px 0;
  background: var(--ifm-background-surface-color);
}

.label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--ifm-color-emphasis-600);
  margin-bottom: 8px;
  font-weight: 600;
}

.commandRow {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.command {
  font-size: 0.9rem;
  flex: 1;
  background: none;
  border: none;
  padding: 0;
}

.copyButton {
  background: none;
  border: 1px solid var(--ifm-color-emphasis-300);
  border-radius: 4px;
  padding: 4px 8px;
  cursor: pointer;
  font-size: 0.85rem;
}

.copyButton:hover {
  background: var(--ifm-color-emphasis-200);
}
```

- [ ] **Step 2: Create ProviderComparison component**

`website/src/components/ProviderComparison/index.tsx`:

```tsx
import React from 'react';
import styles from './styles.module.css';

interface Provider {
  name: string;
  packageName: string;
  features: Record<string, boolean | string>;
}

interface ProviderComparisonProps {
  title: string;
  features: string[];
  providers: Provider[];
}

export default function ProviderComparison({title, features, providers}: ProviderComparisonProps): JSX.Element {
  return (
    <div className={styles.container}>
      <h3 className={styles.title}>{title}</h3>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Feature</th>
            {providers.map((p) => (
              <th key={p.name}>{p.name}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {features.map((feature) => (
            <tr key={feature}>
              <td>{feature}</td>
              {providers.map((p) => (
                <td key={p.name} className={styles.featureCell}>
                  {typeof p.features[feature] === 'boolean'
                    ? p.features[feature] ? '✅' : '❌'
                    : p.features[feature] || '—'}
                </td>
              ))}
            </tr>
          ))}
          <tr className={styles.packageRow}>
            <td><strong>Package</strong></td>
            {providers.map((p) => (
              <td key={p.name}><code>{p.packageName}</code></td>
            ))}
          </tr>
        </tbody>
      </table>
    </div>
  );
}
```

`website/src/components/ProviderComparison/styles.module.css`:

```css
.container {
  margin: 24px 0;
}

.title {
  margin-bottom: 12px;
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.featureCell {
  text-align: center;
}

.packageRow td {
  border-top: 2px solid var(--ifm-color-emphasis-300);
}
```

- [ ] **Step 3: Verify build compiles the components**

```bash
cd website && pnpm build 2>&1 | head -50
```

- [ ] **Step 4: Commit**

```bash
git add website/src/components/NuGetInstall/ website/src/components/ProviderComparison/
git commit -m "feat: add NuGetInstall and ProviderComparison components"
```

---

### Task 10: Build the marketing landing page

**Files:**
- Create: `website/src/pages/index.tsx`
- Create: `website/src/pages/index.module.css`

- [ ] **Step 1: Create the landing page component**

`website/src/pages/index.tsx`:

```tsx
import React from 'react';
import clsx from 'clsx';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import styles from './index.module.css';

const features = [
  { icon: '🗄️', title: 'Persistence', description: 'Repository, Unit of Work, Specifications — with EF Core, Dapper, and Linq2Db providers', link: '/docs/persistence' },
  { icon: '⚡', title: 'CQRS & Mediator', description: 'Command/Query Bus with MediatR and Wolverine implementations', link: '/docs/cqrs-mediator' },
  { icon: '📡', title: 'Event Handling', description: 'In-memory and distributed events with transactional outbox support', link: '/docs/event-handling' },
  { icon: '📬', title: 'Messaging', description: 'Message bus with MassTransit and Wolverine, including state machines', link: '/docs/messaging' },
  { icon: '💾', title: 'Caching', description: 'Unified caching abstraction with Memory and Redis providers', link: '/docs/caching' },
  { icon: '🏗️', title: 'Domain-Driven Design', description: 'Entities, Aggregates, Domain Events, Auditing, and Soft Delete', link: '/docs/domain-driven-design' },
  { icon: '☁️', title: 'Blob Storage', description: 'Azure Blob Storage and Amazon S3 behind a unified abstraction', link: '/docs/blob-storage' },
  { icon: '🏢', title: 'Multi-Tenancy', description: 'Tenant resolution and isolation with Finbuckle integration', link: '/docs/multi-tenancy' },
  { icon: '✅', title: 'Validation & More', description: 'FluentValidation, Email (SMTP/SendGrid), JSON serialization, Security', link: '/docs/validation' },
];

const stats = [
  { value: '37+', label: 'NuGet Packages' },
  { value: '3', label: 'Target Frameworks' },
  { value: '20+', label: 'Working Examples' },
  { value: 'Apache 2.0', label: 'License' },
];

const architectures = [
  { icon: '🏛️', title: 'Clean Architecture', description: 'Layer your application with clear boundaries', link: '/docs/architecture-guides/clean-architecture' },
  { icon: '🔀', title: 'Microservices', description: 'Distributed systems with messaging and events', link: '/docs/architecture-guides/microservices' },
  { icon: '📡', title: 'Event-Driven', description: 'React to domain events across bounded contexts', link: '/docs/architecture-guides/event-driven' },
];

function HeroSection() {
  return (
    <div className={styles.hero}>
      <div className={styles.heroLabel}>Open Source .NET Infrastructure</div>
      <h1 className={styles.heroTitle}>
        Build Enterprise Applications<br />Without Reinventing the Wheel
      </h1>
      <p className={styles.heroSubtitle}>
        RCommon provides battle-tested abstractions for persistence, CQRS, event handling,
        messaging, caching, and more — so you can focus on your domain, not your plumbing.
      </p>
      <div className={styles.heroCta}>
        <Link className={clsx('button button--primary button--lg', styles.ctaPrimary)} to="/docs/getting-started">
          Get Started →
        </Link>
        <Link className={clsx('button button--outline button--lg', styles.ctaSecondary)} to="https://github.com/RCommon-Team/RCommon">
          View on GitHub
        </Link>
      </div>
      <div className={styles.heroInstall}>
        <code>dotnet add package RCommon.Core</code>
      </div>
    </div>
  );
}

function StatsSection() {
  return (
    <div className={styles.stats}>
      {stats.map((stat) => (
        <div key={stat.label} className={styles.stat}>
          <div className={styles.statValue}>{stat.value}</div>
          <div className={styles.statLabel}>{stat.label}</div>
        </div>
      ))}
    </div>
  );
}

function FeaturesSection() {
  return (
    <div className={styles.features}>
      <h2 className={styles.sectionTitle}>Everything You Need</h2>
      <p className={styles.sectionSubtitle}>Pluggable abstractions with multiple provider implementations</p>
      <div className={styles.featureGrid}>
        {features.map((feature) => (
          <Link key={feature.title} to={feature.link} className={styles.featureCard}>
            <div className={styles.featureIcon}>{feature.icon}</div>
            <h3>{feature.title}</h3>
            <p>{feature.description}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}

function CodeSection() {
  return (
    <div className={styles.codeSection}>
      <h2 className={styles.sectionTitle}>Simple, Fluent Configuration</h2>
      <p className={styles.sectionSubtitle}>Configure everything through a single fluent builder in Program.cs</p>
      <pre className={styles.codeBlock}>
        <code>{`builder
    .AddRCommon()
    .WithPersistence<EfCorePeristenceBuilder>(ef =>
        ef.AddDbContext<AppDbContext>(...))
    .WithMediator<MediatRBuilder>(mediator =>
        mediator.AddRequest<CreateOrderCommand>())
    .WithEventHandling<MassTransitBuilder>(events =>
        events.AddProducer<OrderCreatedEvent>())
    .WithCaching<MemoryCacheBuilder>()
    .WithValidation<FluentValidationBuilder>();`}</code>
      </pre>
    </div>
  );
}

function ArchitectureSection() {
  return (
    <div className={styles.architectures}>
      <h2 className={styles.sectionTitle}>Built for Any Architecture</h2>
      <p className={styles.sectionSubtitle}>RCommon fits into the architecture you choose</p>
      <div className={styles.archGrid}>
        {architectures.map((arch) => (
          <Link key={arch.title} to={arch.link} className={styles.archCard}>
            <div className={styles.archIcon}>{arch.icon}</div>
            <h3>{arch.title}</h3>
            <p>{arch.description}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}

export default function Home(): JSX.Element {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout title={siteConfig.title} description={siteConfig.tagline}>
      <main>
        <HeroSection />
        <StatsSection />
        <FeaturesSection />
        <CodeSection />
        <ArchitectureSection />
      </main>
    </Layout>
  );
}
```

- [ ] **Step 2: Create landing page styles**

`website/src/pages/index.module.css`:

```css
.hero {
  text-align: center;
  padding: 80px 24px 48px;
}

.heroLabel {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 2px;
  color: var(--ifm-color-primary);
  margin-bottom: 12px;
  font-weight: 600;
}

.heroTitle {
  font-size: 2.5rem;
  line-height: 1.2;
  margin-bottom: 16px;
}

.heroSubtitle {
  color: var(--ifm-color-emphasis-700);
  max-width: 640px;
  margin: 0 auto 24px;
  font-size: 1.1rem;
  line-height: 1.6;
}

.heroCta {
  display: flex;
  gap: 12px;
  justify-content: center;
  margin-bottom: 16px;
}

.ctaPrimary {
  background: var(--ifm-color-primary);
  color: white;
}

.ctaSecondary {
  border-color: var(--ifm-color-emphasis-400);
}

.heroInstall {
  margin-top: 16px;
}

.heroInstall code {
  background: var(--ifm-background-surface-color);
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 0.9rem;
}

/* Stats */
.stats {
  display: flex;
  justify-content: center;
  gap: 48px;
  padding: 24px;
  border-top: 1px solid var(--ifm-color-emphasis-200);
  border-bottom: 1px solid var(--ifm-color-emphasis-200);
  background: var(--ifm-background-surface-color);
}

.stat {
  text-align: center;
}

.statValue {
  font-size: 1.5rem;
  font-weight: bold;
  color: var(--ifm-color-primary);
}

.statLabel {
  font-size: 0.75rem;
  color: var(--ifm-color-emphasis-600);
}

/* Features */
.features {
  padding: 48px 24px;
  max-width: 1200px;
  margin: 0 auto;
}

.sectionTitle {
  text-align: center;
  font-size: 1.5rem;
  margin-bottom: 8px;
}

.sectionSubtitle {
  text-align: center;
  color: var(--ifm-color-emphasis-600);
  margin-bottom: 32px;
}

.featureGrid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.featureCard {
  background: var(--ifm-background-surface-color);
  border: 1px solid var(--ifm-color-emphasis-200);
  border-radius: 8px;
  padding: 24px;
  text-decoration: none;
  color: inherit;
  transition: border-color 0.2s;
}

.featureCard:hover {
  border-color: var(--ifm-color-primary);
  text-decoration: none;
  color: inherit;
}

.featureIcon {
  font-size: 1.5rem;
  margin-bottom: 8px;
}

.featureCard h3 {
  font-size: 1rem;
  margin-bottom: 4px;
}

.featureCard p {
  color: var(--ifm-color-emphasis-600);
  font-size: 0.85rem;
  margin: 0;
}

/* Code section */
.codeSection {
  padding: 48px 24px;
  background: var(--ifm-background-surface-color);
}

.codeBlock {
  max-width: 680px;
  margin: 0 auto;
  border-radius: 8px;
  padding: 24px;
  font-size: 0.9rem;
  overflow-x: auto;
}

/* Architecture */
.architectures {
  padding: 48px 24px;
  max-width: 1200px;
  margin: 0 auto;
}

.archGrid {
  display: flex;
  justify-content: center;
  gap: 24px;
}

.archCard {
  text-align: center;
  padding: 24px;
  background: var(--ifm-background-surface-color);
  border: 1px solid var(--ifm-color-emphasis-200);
  border-radius: 8px;
  flex: 1;
  max-width: 240px;
  text-decoration: none;
  color: inherit;
  transition: border-color 0.2s;
}

.archCard:hover {
  border-color: var(--ifm-color-primary);
  text-decoration: none;
  color: inherit;
}

.archIcon {
  font-size: 2rem;
  margin-bottom: 8px;
}

.archCard h3 {
  font-size: 1rem;
}

.archCard p {
  color: var(--ifm-color-emphasis-600);
  font-size: 0.85rem;
  margin: 0;
}

/* Responsive */
@media (max-width: 768px) {
  .heroTitle {
    font-size: 1.75rem;
  }

  .featureGrid {
    grid-template-columns: 1fr;
  }

  .stats {
    flex-wrap: wrap;
    gap: 24px;
  }

  .archGrid {
    flex-direction: column;
    align-items: center;
  }

  .heroCta {
    flex-direction: column;
    align-items: center;
  }
}
```

- [ ] **Step 3: Verify the landing page builds**

```bash
cd website && pnpm build 2>&1 | tail -20
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add website/src/pages/
git commit -m "feat: build marketing landing page with hero, features, and code sections"
```

---

### Task 11: Create docs directory structure with stub pages

**Files:**
- Create: All `website/docs/` directories and stub MDX files per the sidebar structure

This task creates every documentation page as a stub (title + placeholder text) so the sidebar works and the build succeeds. Content will be filled in Phase 2.

- [ ] **Step 1: Create all docs directories**

```bash
cd website && mkdir -p docs/getting-started docs/core-concepts docs/domain-driven-design docs/persistence docs/cqrs-mediator docs/event-handling docs/messaging docs/state-machines docs/caching docs/blob-storage docs/serialization docs/validation docs/email docs/multi-tenancy docs/security-web docs/architecture-guides docs/examples-recipes docs/testing docs/api-reference
```

- [ ] **Step 2: Create stub MDX files for every page**

Each stub follows this template:

```mdx
---
title: [Page Title]
sidebar_position: [N]
---

# [Page Title]

*Documentation coming soon.*
```

Create stubs for all pages listed in `sidebars.ts`. The full list (62 files):

**getting-started/**: `overview.mdx`, `installation.mdx`, `quick-start.mdx`, `configuration.mdx`, `dependency-injection.mdx`

**core-concepts/**: `fluent-configuration.mdx`, `guards.mdx`, `guid-generation.mdx`, `system-time.mdx`, `execution-results.mdx`

**domain-driven-design/**: `entities-aggregates.mdx`, `domain-events.mdx`, `value-objects.mdx`, `auditing.mdx`, `soft-delete.mdx`

**persistence/**: `repository-pattern.mdx`, `specifications.mdx`, `unit-of-work.mdx`, `efcore.mdx`, `dapper.mdx`, `linq2db.mdx`, `caching-memory.mdx`, `caching-redis.mdx`

**cqrs-mediator/**: `command-query-bus.mdx`, `commands-handlers.mdx`, `queries-handlers.mdx`, `mediatr.mdx`, `wolverine.mdx`

**event-handling/**: `overview.mdx`, `in-memory.mdx`, `distributed.mdx`, `transactional-outbox.mdx`, `mediatr.mdx`, `masstransit.mdx`, `wolverine.mdx`

**messaging/**: `overview.mdx`, `transactional-outbox.mdx`, `state-machines.mdx`, `masstransit.mdx`, `wolverine.mdx`

**state-machines/**: `overview.mdx`, `stateless.mdx`

**caching/**: `overview.mdx`, `memory.mdx`, `redis.mdx`

**blob-storage/**: `overview.mdx`, `azure.mdx`, `s3.mdx`

**serialization/**: `overview.mdx`, `newtonsoft.mdx`, `system-text-json.mdx`

**validation/**: `fluent-validation.mdx`

**email/**: `overview.mdx`, `sendgrid.mdx`

**multi-tenancy/**: `overview.mdx`, `finbuckle.mdx`

**security-web/**: `authorization.mdx`, `web-utilities.mdx`

**architecture-guides/**: `clean-architecture.mdx`, `microservices.mdx`, `event-driven.mdx`

**examples-recipes/**: `hr-leave-management.mdx`, `event-handling.mdx`, `caching.mdx`, `messaging.mdx`

**testing/**: `overview.mdx`, `test-base-classes.mdx`

**api-reference/**: `nuget-packages.mdx`, `changelog.mdx`, `migration-guide.mdx`

- [ ] **Step 3: Verify the build succeeds with all stubs**

```bash
cd website && pnpm build 2>&1 | tail -20
```

Expected: Build succeeds with no broken link errors.

- [ ] **Step 4: Commit**

```bash
git add website/docs/
git commit -m "feat: create docs directory structure with stub pages for all 62 documentation pages"
```

---

### Task 12: Create GitHub Actions deployment workflow

**Files:**
- Create: `.github/workflows/deploy-website.yml`

- [ ] **Step 1: Create the deployment workflow**

`.github/workflows/deploy-website.yml`:

```yaml
name: Deploy Website to GitHub Pages

on:
  push:
    branches: [main]
    paths:
      - 'website/**'
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: website
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v4
        with:
          version: 9

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: pnpm
          cache-dependency-path: website/pnpm-lock.yaml

      - name: Install dependencies
        run: pnpm install --frozen-lockfile

      - name: Build website
        run: pnpm build

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: website/build

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/deploy-website.yml
git commit -m "ci: add GitHub Actions workflow for website deployment to GitHub Pages"
```

---

### Task 13: Set up local search plugin (Algolia fallback)

**Files:**
- Modify: `website/docusaurus.config.ts`

- [ ] **Step 1: Add local search plugin to docusaurus config**

Add to the `themes` array in `docusaurus.config.ts`:

```typescript
themes: [
  '@docusaurus/theme-mermaid',
  [
    require.resolve('@easyops-cn/docusaurus-search-local'),
    {
      hashed: true,
      language: ['en'],
      highlightSearchTermsOnTargetPage: true,
      explicitSearchResultPath: true,
    },
  ],
],
```

- [ ] **Step 2: Verify build with search plugin**

```bash
cd website && pnpm build 2>&1 | tail -20
```

Expected: Build succeeds. Search index is generated.

- [ ] **Step 3: Commit**

```bash
git add website/docusaurus.config.ts
git commit -m "feat: add local search plugin as Algolia fallback"
```

---

### Task 14: First full build and local dev verification

- [ ] **Step 1: Run full build**

```bash
cd website && pnpm build
```

Expected: Build completes successfully with no errors.

- [ ] **Step 2: Start local dev server and verify**

```bash
cd website && pnpm start
```

Verify in browser at http://localhost:3000:
- Landing page renders with hero, stats, features, code, architecture sections
- Clicking "Get Started" navigates to docs
- Sidebar shows all 19 categories with expandable sub-items
- Dark/light mode toggle works
- Search bar appears and is functional
- All stub pages render

- [ ] **Step 3: Fix any build or rendering issues found**

- [ ] **Step 4: Commit any fixes**

```bash
git add -A website/
git commit -m "fix: resolve build and rendering issues from initial integration"
```

---

## Phase 2: Documentation Content

Each task in this phase fills in documentation content for a sidebar section. For every page, follow the page template from the spec:

1. **Overview** — What this feature is and when to use it
2. **Installation** — NuGet package(s) via `<NuGetInstall packageName="..." />`
3. **Configuration** — Fluent builder setup in `Program.cs`
4. **Usage** — Code examples showing common patterns
5. **Provider tabs** — Side-by-side using `<Tabs>` where applicable
6. **Advanced usage** — Edge cases, customization
7. **API summary** — Key interfaces/classes

**Content source priority:** Source code → Package READMEs → Examples → Tests → Existing docs.rcommon.com

**MDX component imports available:**

```mdx
import NuGetInstall from '@site/src/components/NuGetInstall';
import ProviderComparison from '@site/src/components/ProviderComparison';
import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';
```

---

### Task 15: Getting Started section (5 pages)

**Files:**
- Modify: `website/docs/getting-started/overview.mdx`
- Modify: `website/docs/getting-started/installation.mdx`
- Modify: `website/docs/getting-started/quick-start.mdx`
- Modify: `website/docs/getting-started/configuration.mdx`
- Modify: `website/docs/getting-started/dependency-injection.mdx`

**Source files to read:**
- `Src/RCommon.Core/README.md` — core package overview and install
- `Src/RCommon.Core/RCommonBuilder.cs` — fluent builder entry point
- `Src/RCommon.Core/Configuration/` — configuration classes
- `Src/RCommon.Core/Microsoft/` — DI extension methods
- `Examples/CleanWithCQRS/` — real-world usage of configuration
- Existing docs at https://docs.rcommon.com (fundamentals section)

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — What RCommon is, philosophy, when to use it, comparison with alternatives
- [ ] **Step 3: Write installation.mdx** — NuGet packages, target framework requirements (.NET 8/9/10), project setup
- [ ] **Step 4: Write quick-start.mdx** — Minimal working example with EF Core persistence + MediatR
- [ ] **Step 5: Write configuration.mdx** — Fluent builder API, `AddRCommon()`, chaining `With*()` methods
- [ ] **Step 6: Write dependency-injection.mdx** — How RCommon uses DI, service registration, lifetimes
- [ ] **Step 7: Verify build**

```bash
cd website && pnpm build 2>&1 | tail -10
```

- [ ] **Step 8: Commit**

```bash
git add website/docs/getting-started/
git commit -m "docs: write Getting Started section (5 pages)"
```

---

### Task 16: Core Concepts section (5 pages)

**Files:**
- Modify: `website/docs/core-concepts/*.mdx` (5 files)

**Source files to read:**
- `Src/RCommon.Core/Guards/` — Guard clause implementations
- `Src/RCommon.Core/GuidGeneration/` — GUID generation strategies
- `Src/RCommon.Core/SystemTime/` — System time abstraction
- `Src/RCommon.Core/RCommonBuilder.cs` — fluent configuration
- `Src/RCommon.Models/` — ExecutionResult, pagination, DTOs

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write fluent-configuration.mdx** — The builder pattern, `IRCommonBuilder`, method chaining
- [ ] **Step 3: Write guards.mdx** — Guard clauses, usage patterns, available guards
- [ ] **Step 4: Write guid-generation.mdx** — GUID strategies, `IGuidGenerator`, sequential GUIDs
- [ ] **Step 5: Write system-time.mdx** — `ISystemTime`, testability, clock abstraction
- [ ] **Step 6: Write execution-results.mdx** — `ExecutionResult`, pagination models, DTOs
- [ ] **Step 7: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/core-concepts/ && git commit -m "docs: write Core Concepts section (5 pages)"
```

---

### Task 17: Domain-Driven Design section (5 pages)

**Files:**
- Modify: `website/docs/domain-driven-design/*.mdx` (5 files)

**Source files to read:**
- `Src/RCommon.Entities/` — Entity, AggregateRoot, IAuditedEntity, ISoftDeletable
- `Src/RCommon.Core/EventHandling/` — domain event base classes
- `docs/superpowers/specs/2026-03-16-ddd-entity-abstractions-design.md` — DDD design spec
- `Tests/RCommon.Entities.Tests/` — entity behavior tests

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write entities-aggregates.mdx** — Entity base, AggregateRoot, identity, equality
- [ ] **Step 3: Write domain-events.mdx** — Domain event pattern, `IDomainEvent`, raising events from entities
- [ ] **Step 4: Write value-objects.mdx** — Value object pattern and implementation
- [ ] **Step 5: Write auditing.mdx** — `IAuditedEntity`, `ICreatedAuditedEntity`, `IUpdatedAuditedEntity`, auto-tracking
- [ ] **Step 6: Write soft-delete.mdx** — `ISoftDeletable`, query filtering, implementation
- [ ] **Step 7: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/domain-driven-design/ && git commit -m "docs: write Domain-Driven Design section (5 pages)"
```

---

### Task 18: Persistence section (8 pages)

**Files:**
- Modify: `website/docs/persistence/*.mdx` (8 files)

**Source files to read:**
- `Src/RCommon.Persistence/` — IRepository, IUnitOfWork, ISpecification
- `Src/RCommon.EfCore/` — EF Core repository implementation
- `Src/RCommon.Dapper/` — Dapper implementation
- `Src/RCommon.Linq2Db/` — Linq2Db implementation
- `Src/RCommon.Persistence.Caching/` — caching layer
- `Src/RCommon.Persistence.Caching.MemoryCache/`
- `Src/RCommon.Persistence.Caching.RedisCache/`
- `Examples/CleanWithCQRS/` — persistence usage in Clean Architecture example
- Existing docs at https://docs.rcommon.com (persistence section)

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write repository-pattern.mdx** — `IGraphRepository`, `ILinqRepository`, CRUD operations, generic vs specific
- [ ] **Step 3: Write specifications.mdx** — Specification pattern, building queries, combining specs
- [ ] **Step 4: Write unit-of-work.mdx** — `IUnitOfWorkFactory`, `IUnitOfWorkScope`, transaction management
- [ ] **Step 5: Write efcore.mdx** — EF Core setup, `EfCoreRepository`, DbContext configuration, provider-specific tab content
- [ ] **Step 6: Write dapper.mdx** — Dapper setup, `DapperRepository`, raw SQL support
- [ ] **Step 7: Write linq2db.mdx** — Linq2Db setup, `Linq2DbRepository`
- [ ] **Step 8: Write caching-memory.mdx** — Persistence caching with MemoryCache
- [ ] **Step 9: Write caching-redis.mdx** — Persistence caching with Redis
- [ ] **Step 10: Add ProviderComparison to repository-pattern.mdx** — EF Core vs Dapper vs Linq2Db feature matrix
- [ ] **Step 11: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/persistence/ && git commit -m "docs: write Persistence section (8 pages)"
```

---

### Task 19: CQRS & Mediator section (5 pages)

**Files:**
- Modify: `website/docs/cqrs-mediator/*.mdx` (5 files)

**Source files to read:**
- `Src/RCommon.ApplicationServices/` — command/query bus
- `Src/RCommon.Mediator/` — mediator abstraction
- `Src/RCommon.Mediatr/` — MediatR implementation
- `Src/RCommon.Wolverine/` — Wolverine implementation
- `Examples/ApplicationServices/` — CQRS examples
- `Examples/Mediator/` — mediator examples
- Existing docs at https://docs.rcommon.com (CQRS, mediator sections)

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write command-query-bus.mdx** — CQRS pattern, `ICommandBus`, `IQueryBus`
- [ ] **Step 3: Write commands-handlers.mdx** — Defining commands, creating handlers, registration
- [ ] **Step 4: Write queries-handlers.mdx** — Defining queries, creating handlers, return types
- [ ] **Step 5: Write mediatr.mdx** — MediatR setup, pipeline behaviors, notifications
- [ ] **Step 6: Write wolverine.mdx** — Wolverine setup, handler conventions, middleware
- [ ] **Step 7: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/cqrs-mediator/ && git commit -m "docs: write CQRS & Mediator section (5 pages)"
```

---

### Task 20: Event Handling section (7 pages)

**Files:**
- Modify: `website/docs/event-handling/*.mdx` (6 files)

**Source files to read:**
- `Src/RCommon.Core/EventHandling/` — event bus abstraction
- `Src/RCommon.Mediatr/` — MediatR event handling
- `Src/RCommon.MassTransit/` — MassTransit event handling
- `Src/RCommon.Wolverine/` — Wolverine event handling
- `Src/RCommon.MassTransit.Outbox/` — transactional outbox
- `Src/RCommon.Wolverine.Outbox/` — Wolverine outbox
- `Examples/EventHandling/` — event handling examples
- `docs/superpowers/specs/2026-03-21-transactional-outbox-design.md`
- `docs/superpowers/specs/2026-03-23-outbox-v2-design.md`

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — Event handling patterns, when to use which approach
- [ ] **Step 3: Write in-memory.mdx** — In-memory event bus, local pub/sub
- [ ] **Step 4: Write distributed.mdx** — Distributed events across services
- [ ] **Step 5: Write transactional-outbox.mdx** — Outbox pattern, guaranteeing delivery
- [ ] **Step 6: Write mediatr.mdx** — MediatR notifications as events
- [ ] **Step 7: Write masstransit.mdx** — MassTransit consumers, publish/subscribe
- [ ] **Step 8: Write wolverine.mdx** — Wolverine event handling
- [ ] **Step 9: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/event-handling/ && git commit -m "docs: write Event Handling section (6 pages)"
```

---

### Task 21: Messaging section (5 pages)

**Files:**
- Modify: `website/docs/messaging/*.mdx` (5 files)

**Source files to read:**
- `Src/RCommon.MassTransit/` — MassTransit messaging
- `Src/RCommon.MassTransit.Outbox/` — outbox for messaging
- `Src/RCommon.MassTransit.StateMachines/` — saga state machines
- `Src/RCommon.Wolverine/` — Wolverine messaging
- `Src/RCommon.Wolverine.Outbox/` — Wolverine outbox
- `Examples/Messaging/` — messaging examples

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — Message bus vs event bus, when to use messaging
- [ ] **Step 3: Write transactional-outbox.mdx** — Outbox pattern for messaging reliability
- [ ] **Step 4: Write state-machines.mdx** — MassTransit sagas, state machine definition
- [ ] **Step 5: Write masstransit.mdx** — MassTransit setup, consumers, producers, configuration
- [ ] **Step 6: Write wolverine.mdx** — Wolverine setup, message handlers, durable messaging
- [ ] **Step 7: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/messaging/ && git commit -m "docs: write Messaging section (5 pages)"
```

---

### Task 22: State Machines section (2 pages)

**Files:**
- Modify: `website/docs/state-machines/*.mdx` (2 files)

**Source files to read:**
- `Src/RCommon.Stateless/` — Stateless integration

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — State machine pattern, Stateless vs MassTransit sagas
- [ ] **Step 3: Write stateless.mdx** — Stateless library integration, defining states/triggers
- [ ] **Step 4: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/state-machines/ && git commit -m "docs: write State Machines section (2 pages)"
```

---

### Task 23: Caching section (3 pages)

**Files:**
- Modify: `website/docs/caching/*.mdx` (3 files)

**Source files to read:**
- `Src/RCommon.Caching/` — caching abstraction
- `Src/RCommon.MemoryCache/` — memory cache implementation
- `Src/RCommon.RedisCache/` — Redis implementation
- `Examples/Caching/` — caching examples

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — Caching abstraction, `ICacheService`, when to use caching
- [ ] **Step 3: Write memory.mdx** — MemoryCache setup, usage, eviction policies
- [ ] **Step 4: Write redis.mdx** — Redis setup, connection configuration, distributed caching
- [ ] **Step 5: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/caching/ && git commit -m "docs: write Caching section (3 pages)"
```

---

### Task 24: Blob Storage section (3 pages)

**Files:**
- Modify: `website/docs/blob-storage/*.mdx` (3 files)

**Source files to read:**
- `Src/RCommon.Blobs/` — blob storage abstraction
- `Src/RCommon.Azure.Blobs/` — Azure implementation
- `Src/RCommon.Amazon.S3Objects/` — S3 implementation
- `docs/superpowers/specs/2026-03-23-blob-storage-design.md` — blob storage design spec

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — Blob storage abstraction, `IBlobStorageService`, operations
- [ ] **Step 3: Write azure.mdx** — Azure Blob Storage setup, connection strings, container config
- [ ] **Step 4: Write s3.mdx** — Amazon S3 setup, credentials, bucket configuration
- [ ] **Step 5: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/blob-storage/ && git commit -m "docs: write Blob Storage section (3 pages)"
```

---

### Task 25: Serialization section (3 pages)

**Files:**
- Modify: `website/docs/serialization/*.mdx` (3 files)

**Source files to read:**
- `Src/RCommon.Json/` — JSON abstraction
- `Src/RCommon.JsonNet/` — Newtonsoft.Json implementation
- `Src/RCommon.SystemTextJson/` — System.Text.Json implementation
- `Examples/Json/` — JSON examples

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — JSON abstraction, `IJsonSerializer`, why abstract serialization
- [ ] **Step 3: Write newtonsoft.mdx** — Newtonsoft.Json setup, custom settings
- [ ] **Step 4: Write system-text-json.mdx** — System.Text.Json setup, `JsonSerializerOptions`
- [ ] **Step 5: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/serialization/ && git commit -m "docs: write Serialization section (3 pages)"
```

---

### Task 26: Validation section (1 page)

**Files:**
- Modify: `website/docs/validation/fluent-validation.mdx`

**Source files to read:**
- `Src/RCommon.FluentValidation/` — FluentValidation integration
- `Examples/Validation/` — validation examples

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write fluent-validation.mdx** — Setup, creating validators, pipeline integration, validation behavior
- [ ] **Step 3: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/validation/ && git commit -m "docs: write Validation section (1 page)"
```

---

### Task 27: Email section (2 pages)

**Files:**
- Modify: `website/docs/email/*.mdx` (2 files)

**Source files to read:**
- `Src/RCommon.Emailing/` — email abstraction (includes SMTP)
- `Src/RCommon.SendGrid/` — SendGrid implementation

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — Email abstraction, `IEmailService`, built-in SMTP support, configuration
- [ ] **Step 3: Write sendgrid.mdx** — SendGrid setup, API key config, template support
- [ ] **Step 4: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/email/ && git commit -m "docs: write Email section (2 pages)"
```

---

### Task 28: Multi-Tenancy section (2 pages)

**Files:**
- Modify: `website/docs/multi-tenancy/*.mdx` (2 files)

**Source files to read:**
- `Src/RCommon.MultiTenancy/` — multi-tenancy abstraction
- `Src/RCommon.Finbuckle/` — Finbuckle integration
- Recent commit #158 for multi-tenancy context

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — Multi-tenancy concepts, tenant resolution strategies
- [ ] **Step 3: Write finbuckle.mdx** — Finbuckle setup, tenant stores, resolution strategies
- [ ] **Step 4: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/multi-tenancy/ && git commit -m "docs: write Multi-Tenancy section (2 pages)"
```

---

### Task 29: Security & Web section (2 pages)

**Files:**
- Modify: `website/docs/security-web/*.mdx` (2 files)

**Source files to read:**
- `Src/RCommon.Security/` — security abstractions
- `Src/RCommon.Authorization.Web/` — web authorization
- `Src/RCommon.Web/` — web utilities

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write authorization.mdx** — Authorization helpers, claim-based access
- [ ] **Step 3: Write web-utilities.mdx** — HTTP utilities, web helpers
- [ ] **Step 4: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/security-web/ && git commit -m "docs: write Security & Web section (2 pages)"
```

---

### Task 30: Architecture Guides section (3 pages)

**Files:**
- Modify: `website/docs/architecture-guides/*.mdx` (3 files)

**Source files to read:**
- `Examples/CleanWithCQRS/` — Clean Architecture example
- `Examples/Messaging/` — distributed messaging examples
- `Examples/EventHandling/` — event-driven examples
- Existing docs at https://docs.rcommon.com (architecture section)

- [ ] **Step 1: Read source files and existing docs for context**
- [ ] **Step 2: Write clean-architecture.mdx** — Clean Architecture with RCommon, layers, dependency rule, example walkthrough
- [ ] **Step 3: Write microservices.mdx** — Microservices with RCommon, messaging between services, shared abstractions
- [ ] **Step 4: Write event-driven.mdx** — Event-driven architecture, event sourcing concepts, outbox pattern in practice
- [ ] **Step 5: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/architecture-guides/ && git commit -m "docs: write Architecture Guides section (3 pages)"
```

---

### Task 31: Examples & Recipes section (4 pages)

**Files:**
- Modify: `website/docs/examples-recipes/*.mdx` (4 files)

**Source files to read:**
- `Examples/CleanWithCQRS/` — HR Leave Management system
- `Examples/EventHandling/` — event handling examples
- `Examples/Caching/` — caching examples
- `Examples/Messaging/` — messaging examples

- [ ] **Step 1: Read example projects for context**
- [ ] **Step 2: Write hr-leave-management.mdx** — Full walkthrough of the Clean + CQRS example, project structure, how it all fits together
- [ ] **Step 3: Write event-handling.mdx** — Step-by-step event handling recipe
- [ ] **Step 4: Write caching.mdx** — Caching recipe with provider comparison
- [ ] **Step 5: Write messaging.mdx** — Messaging recipe with MassTransit and Wolverine
- [ ] **Step 6: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/examples-recipes/ && git commit -m "docs: write Examples & Recipes section (4 pages)"
```

---

### Task 32: Testing section (2 pages)

**Files:**
- Modify: `website/docs/testing/*.mdx` (2 files)

**Source files to read:**
- `Tests/RCommon.TestBase/` — test base classes
- `Tests/RCommon.TestBase.Data/` — test data utilities
- `Tests/RCommon.TestBase.XUnit/` — XUnit utilities
- `Tests/` — example test patterns from the test suite

- [ ] **Step 1: Read source files for context**
- [ ] **Step 2: Write overview.mdx** — Testing strategy with RCommon, mocking abstractions, integration testing
- [ ] **Step 3: Write test-base-classes.mdx** — `TestBase`, data helpers, XUnit integration
- [ ] **Step 4: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/testing/ && git commit -m "docs: write Testing section (2 pages)"
```

---

### Task 33: API Reference section (3 pages)

**Files:**
- Modify: `website/docs/api-reference/*.mdx` (3 files)

**Source files to read:**
- All `Src/*/README.md` files — package descriptions
- `Src/RCommon.sln` — full solution for package listing

- [ ] **Step 1: Read package READMEs for context**
- [ ] **Step 2: Write nuget-packages.mdx** — Complete table of all NuGet packages with descriptions, versions, and links
- [ ] **Step 3: Write changelog.mdx** — Recent changes, link to GitHub releases
- [ ] **Step 4: Write migration-guide.mdx** — Upgrading between versions, breaking changes
- [ ] **Step 5: Verify build and commit**

```bash
cd website && pnpm build && git add website/docs/api-reference/ && git commit -m "docs: write API Reference section (3 pages)"
```

---

## Phase 3: Polish & Deploy

### Task 34: Final build verification and broken link check

- [ ] **Step 1: Run full production build**

```bash
cd website && pnpm build
```

Expected: No errors, no warnings about broken links.

- [ ] **Step 2: Serve the production build locally**

```bash
cd website && pnpm serve
```

Verify at http://localhost:3000:
- Landing page renders correctly (all sections, responsive)
- Every sidebar link resolves to a page with content
- Dark/light mode toggle works throughout
- Search finds content across all pages
- All code examples have syntax highlighting
- NuGetInstall components render with copy button
- ProviderComparison tables render correctly
- No console errors in browser

- [ ] **Step 3: Fix any issues found**

- [ ] **Step 4: Commit fixes**

```bash
git add -A website/ && git commit -m "fix: resolve final build and content issues"
```

---

### Task 35: Set up versioning and prepare for deployment

- [ ] **Step 1: Create the first version snapshot**

```bash
cd website && npx docusaurus docs:version 1.0
```

This snapshots the current docs as version 1.0 and makes them the default.

- [ ] **Step 2: Verify version dropdown appears**

```bash
cd website && pnpm build && pnpm serve
```

Check that the version dropdown in the navbar shows "1.0" and "Next".

- [ ] **Step 3: Commit versioned docs**

```bash
git add website/versioned_docs/ website/versioned_sidebars/ website/versions.json
git commit -m "docs: snapshot version 1.0 of documentation"
```

- [ ] **Step 4: Squash interim commits into a single meaningful commit**

Rebase and squash all commits on `feature/website-v2` into a clean history before merging.

- [ ] **Step 5: Push to remote and verify GitHub Pages deployment**

```bash
git push origin feature/website-v2
```

Create a PR to merge into main. After merge, verify the GitHub Actions workflow runs and the site deploys to rcommon.com.

---

## Reminders

- **User-provided assets needed before Task 6:** RCommon logo (SVG), dark mode logo variant, favicon, brand color palette. Placeholders are used until then.
- **Algolia DocSearch:** Apply at https://docsearch.algolia.com/apply/ after launch. Local search works as fallback.
- **GitHub Pages:** Must be enabled in repository settings (Settings → Pages → Source: GitHub Actions) before the first deployment.
- **DNS:** `rcommon.com` must have CNAME record pointing to `rcommon-team.github.io` for GitHub Pages custom domain.
