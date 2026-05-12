---
name: new-playwright-test
description: Generate a Playwright end-to-end spec file for a Mars Staff Portal page. Trigger when the user asks to write, create, or generate a Playwright/E2E/visual-regression/accessibility test for a specific page URL — e.g. "write a playwright test for /waitinglists/-1/", "create a spec for /appointments/", "generate e2e tests for the clinic-setup page", "add a visual regression test for /broadcast/". The user supplies the page URL as input. Do NOT use this skill for unit tests, integration tests, .NET tests (NUnit/xUnit), API tests outside the UI, or for any non-Mars Staff Portal page. Do NOT use it to edit an existing spec — only to create a new one from a URL.
disable-model-invocation: false
---

# Generate a Playwright spec for a Mars Staff Portal page

## Input

Page URL: <PAGE_URL>

## When to use this skill

Use when **all** of the following are true:

- The user is asking for a **new** spec file (not editing an existing one).
- The target is a **UI page** rendered by a Mars Staff Portal app in this repo.
- A **URL** is provided or can be inferred from context.

If any of these are false, do not use this skill. For unit tests, .NET tests, API tests, or edits to an existing spec, decline and direct the user to the relevant tool or write the answer directly.

## Operating principle

This skill describes a **process**, not the data the process operates on. It contains:

- No app names — discovered at runtime by globbing.
- No framework names — patterns detected by reading existing files.
- No URL examples — the algorithm runs on whatever URL was supplied.
- No filename examples — naming inferred from existing specs.

All repo-specific knowledge lives in `.claude/rules/playwright-testing.md` (auto-loaded when working under `DrDoctor.Playwright/`) and in existing spec files. Read those; do not embed their content here.

## Goal

Produce a single Playwright spec file in `DrDoctor.Playwright/tests/` that tests the page at the URL above. The spec MUST conform to every rule in `.claude/rules/playwright-testing.md` — locator priority, no manual waits, web-first assertions, accessibility opt-in mechanism, and the anti-patterns list.

## Pre-flight checks

Before generating anything, confirm:

1. `.claude/rules/playwright-testing.md` exists and is readable. If not, stop and tell the user the rules file is missing.
2. `DrDoctor.Playwright/tests/` exists. If not, stop and tell the user the Playwright workspace is missing.
3. The URL has at least one non-empty path segment. If the URL is just `/` or empty, stop and ask the user to clarify which page they mean.

## Process

### 1. Establish conventions

Read `.claude/rules/playwright-testing.md` in full. Every rule there is mandatory. It describes how to discover apps, framework patterns, configs, and the strict standards every generated spec must meet.

### 2. Find a reference spec to copy

Glob `DrDoctor.Playwright/tests/*.spec.ts`. If at least one exists, read the most recent one and treat it as the canonical template for:

- Imports
- Helper functions
- Test structure, naming, and assertion style
- Filename convention
- How URLs and sub-routes are expressed

If no spec exists yet, follow the rules file in full as the template.

### 3. Identify which app and module owns the URL

Use the discovery techniques described in section 1 of the rules file. Search the apps' backend folders for a handler file whose name matches the URL's first path segment.

If no match is found, stop and report:
> "Could not match the URL `<PAGE_URL>` to any backend handler under `<repo-root>`. Has the page been created yet?"

If the segment matches in multiple apps, pick the first one and note this in your final report so the developer can correct you if wrong.

### 4. Discover the sub-routes

A page may have one URL or many. Read the relevant source files of the owning app to extract every sub-route that applies. If only one URL applies, treat `<PAGE_URL>` itself as the only sub-route.

### 5. Construct the full URLs

For each sub-route, compute the full URL a browser would navigate to. Use `<PAGE_URL>` as the anchor and append sub-route paths exactly as the reference spec from step 2 expresses them.

### 6. Find the matching Playwright config

Glob `DrDoctor.Playwright/playwright.*.config.ts`. Read each; pick the one whose `BASE_URL` (or the `auth.*.setup.ts` it references) corresponds to the owning app from step 3.

### 7. Derive the spec filename

Read the `testMatch` regex of the config from step 6. The chosen filename MUST match it. Also follow the naming convention of existing specs in `tests/`.

### 8. Generate the spec

At the top of the file, declare the accessibility env flag once:

```typescript
const RUN_A11Y = process.env.RUN_A11Y === 'true';
```

For each sub-route, generate a `test.describe` block containing:

1. `test.beforeEach` that navigates to the sub-route's full URL with `{ waitUntil: 'networkidle' }`.
2. **URL test** — `await expect(page).toHaveURL(...)` with a 30 s timeout, also asserting the URL is not on a login redirect. **NEVER use** `page.waitForURL` or `expect(page.url()).toContain(...)` — they fail during redirects.
3. **Screenshot test** — wait for content using the helper from the reference spec, then `toHaveScreenshot('<name>-full.png', { fullPage: true, maxDiffPixels: 100 })`.
4. **Accessibility test — conditionally registered** — wrap in `if (RUN_A11Y) { ... }`. Inside: `AxeBuilder` with `wcag2a` and `wcag2aa` tags; filter to `impact === 'critical'`; document every `.exclude(...)` selector with a comment naming the library defect. Do **NOT use** `test.skip()` — register the test only when the flag is on.

### 9. Locator rules — non-negotiable

All test code MUST follow the locator priority in section 3 of the rules file:

1. `page.getByRole(role, { name })` — first choice for interactive elements.
2. `page.getByLabel(text)` — form inputs with labels.
3. `page.getByPlaceholder(text)` — inputs without labels.
4. `page.getByText(text)` — static text.
5. `page.getByAltText(text)` / `page.getByTitle(text)` / `page.getByTestId(id)`.
6. CSS / XPath — **last resort only**, with a comment explaining why nothing semantic works.

NEVER write `page.click('css-selector')`, `page.locator('.classname')` as the primary locator, `:nth-child` selectors, or `page.waitForTimeout(...)`.

### 10. Self-verification before reporting

Before reporting back, scan the file you just wrote and confirm:

- ✅ No `page.waitForURL(` — replaced by `expect(page).toHaveURL(`
- ✅ No `page.waitForTimeout(` anywhere
- ✅ No `expect(await locator.isVisible())` patterns — must be `expect(locator).toBeVisible()`
- ✅ No `expect(page.url()).toContain(` — must be `expect(page).toHaveURL(`
- ✅ No raw CSS selectors as the primary locator
- ✅ `if (RUN_A11Y)` block exists exactly once per `describe`, the accessibility test is inside it
- ✅ No `test.skip()` referenced anywhere
- ✅ Filename matches the config's `testMatch` regex
- ✅ Every `describe` has a `beforeEach` navigating to the right sub-route

If any check fails, fix the spec before reporting.

### 11. Report back

Reply in this exact shape:

```
Created: <full-path-of-spec-file>

App identified:    <app-name>
Discovery method:  <how you matched the URL to the app>
Framework:         <framework pattern detected>
Sub-routes (N):
  1. <sub-route-1>
  2. <sub-route-2>
  ...

To create baseline screenshots:
  cd DrDoctor.Playwright
  npx playwright test tests/<filename> --config=<config> --update-snapshots

To run with accessibility checks enabled:
  RUN_A11Y=true npx playwright test tests/<filename> --config=<config>

Remember: add a `case` entry in githooks/.githooks/pre-commit so the hook
runs this spec when the source files of <app-name>'s <feature> change.
The existing entries in that file show the pattern.
```

Make reasonable assumptions and document them inline in the report. Do not ask clarifying questions.

## Failure modes — what to do

| Situation | Action |
|---|---|
| Rules file missing | Stop. Report: "Cannot proceed — `.claude/rules/playwright-testing.md` not found." |
| No backend handler matches URL | Stop. Report: "Could not match `<PAGE_URL>` to any handler. Has the page been created yet?" |
| URL matches multiple apps | Pick the first match (alphabetically), proceed, document the disambiguation in the report. |
| No reference spec exists | Follow the rules file exactly as template. Note in report that no reference was available. |
| Self-verification step 10 fails | Fix the spec, re-verify, then report. Do not report a spec that fails self-verification. |
