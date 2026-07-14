# Docusaurus 3.10 Upgrade & Swizzle Migration Design Spec (Follow-up)

> **Scope:** Upgrade the documentation website from Docusaurus 3.4.0 to the latest 3.x (3.10.2+) to clear npm build-tooling vulnerabilities, and migrate the 69 swizzled theme components that the upgrade breaks.

**Date:** 2026-07-14
**Status:** Proposed — deferred follow-up (not done in 3.1.1)
**Trigger:** `pnpm audit` on `website/` reports 58 vulnerabilities against Docusaurus 3.4.0's transitive dependency tree (1 critical, 16 high at time of writing).

---

## 1. Motivation

The `website/` project pins Docusaurus 3.4.0. Its transitive dependency tree carries many known
vulnerabilities (node-forge signature forgery, shell-quote RCE, lodash `_.template` injection, undici,
ws, dompurify, etc.). All are **build-time / dev-server tooling** for a static-site generator — the
generated static site ships no npm code to visitors, so the exposure is limited to CI/build machines —
but they still surface on every `pnpm audit` and should be cleared.

The effective fix is upgrading Docusaurus to the latest 3.x. A trial upgrade to 3.10.2 dropped the count
from 58 to ~2 (with a small set of `pnpm.overrides`) and eliminated the critical and the unpatchable-in-place
advisories. **However, the upgrade is not a drop-in**: it breaks the build.

## 2. Why it was deferred from 3.1.1

The site heavily customizes the theme via **69 swizzled components** under `website/src/theme/`
(Navbar, Footer, DocItem, DocSidebarItem, DocRoot, TOC, Admonition, CodeBlock, AnnouncementBar, …).
Swizzled (ejected) components are tightly coupled to Docusaurus theme internals, which change across
minor releases. The 3.4 → 3.10 jump breaks them — the first observed failure is
`Module not found: Can't resolve '@theme/Unlisted'` in `src/theme/DocItem/Layout`, and more are expected
once that is resolved.

Migrating 69 components and re-validating the site is a focused project of its own, inappropriate to bundle
into a patch release (3.1.1) whose purpose is the cross-host outbox fix. Doing it under time pressure on a
release branch risks subtle visual/behavioural regressions in the docs site.

## 3. Scope of the migration

1. Bump all `@docusaurus/*` packages 3.4.0 → latest 3.x (3.10.2 at time of writing) in
   `website/package.json` (dependencies + devDependencies).
2. Bump `@easyops-cn/docusaurus-search-local` to a version compatible with the target Docusaurus
   (0.55.x). Note: during the trial, a `pnpm` override on `undici`/`cheerio` broke the search plugin's
   server module (`Cannot read properties of undefined (reading 'bind')`) — the override set must be
   curated so it does not perturb the search plugin's dependency tree.
3. Re-swizzle or hand-migrate each of the 69 components under `src/theme/` against the target version.
   For each: diff against the target version's ejected source, re-apply the local customization, and drop
   components that only existed to patch 3.4-era bugs now fixed upstream.
4. Add a minimal, curated `pnpm.overrides` (or `pnpm-workspace.yaml` overrides) block for any residual
   high/critical advisories whose patched version actually exists on npm. Avoid auto-generated override
   sets — `pnpm audit --fix` produced overrides referencing non-existent versions (e.g. lodash `>=4.18.0`,
   serialize-javascript `>=7.0.5`).
5. Rebuild (`pnpm docusaurus build`) to exit 0, then visually verify the site (navbar, sidebar, TOC,
   doc footer, search, admonitions, code-block copy button, announcement bar) and re-cut/refresh any
   docs version snapshots as needed.

## 4. Acceptance criteria

- `pnpm docusaurus build` exits 0.
- `pnpm audit` reports zero critical and zero high (moderate/low in unavoidable build tooling acceptable
  and documented).
- All swizzled theme customizations render as before (manual visual pass).
- The search plugin works (local search index builds and queries).

## 5. Related

- Website: `website/package.json`, `website/src/theme/` (69 swizzled components)
- Changelog note for 3.1.1 records that .NET package vulnerabilities were fixed and website vulns deferred.
