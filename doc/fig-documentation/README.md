# Fig documentation

This site is the Fig product documentation, built with [Docusaurus 3](https://docusaurus.io/). Source lives in this folder; production is published to [figsettings.com](https://www.figsettings.com) via GitHub Pages.

## Local development

Requires Node 20+.

```bash
npm ci
npm start
```

The dev server opens a browser and reloads on most markdown and config changes.

```bash
npm run build
```

Produces a static site in `build/`. Broken page links fail the build (`onBrokenLinks: throw`).

## Deployment

Pushes to `main` that include this folder are built by [`.github/workflows/deploy_documentation.yml`](../../.github/workflows/deploy_documentation.yml) (`npm ci` + `npm run build`) and published to the `gh-pages` branch.

Do not use `npm run deploy` / `GIT_USER` for production; that is the stock Docusaurus GitHub Pages helper and is not how figsettings.com is updated.

## Versions

The default published version is **4.x** (`/docs/`). Markdown in `docs/` is served as **Next** (`/docs/next/`). Archived versions are `4.x`, `3.x`, and `2.0` under `versioned_docs/`. Snapshot a new version with `npx docusaurus docs:version <label>` when that release ships — not on every docs edit.
