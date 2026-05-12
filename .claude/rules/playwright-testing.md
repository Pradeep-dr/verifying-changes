---
paths:
  - "DrDoctor.Playwright/**"
  - "tests/**"
---

# Playwright Testing — Rules

These rules apply to every Playwright spec in this repo. They are not suggestions. Generated and hand-written specs alike MUST follow them.

---

## 1. Workspace architecture

This repo tests multiple web apps from a single shared Playwright workspace under `DrDoctor.Playwright/`.

Discover the structure at runtime — never hardcode app names, framework names, or URL patterns:

- **Apps**: glob `*/src/*.Web/` from the repo root.
- **Framework of an app**: read its source root. The folder containing backend route handlers (whatever the app calls it) and the class types those handlers extend indicate the backend framework. The presence of a client-side scripts folder containing a routing configuration indicates that the app uses single-page sub-routing inside one backend shell rather than per-URL backend routes.
- **Playwright configs**: glob `playwright.*.config.ts`. Each config is paired with a `auth.*.setup.ts` it references via the `setup` project dependency.
- **Reference spec**: the most recent file matching `tests/*.spec.ts` is the canonical example for current conventions. Read it before writing a new spec.

---

## 2. Spec file structure

- **Location**: `DrDoctor.Playwright/tests/`
- **Filename**: Each app's `playwright.<app>.config.ts` has a `testMatch` setting that filters specs by filename. Before naming a new spec, read the matching config and find its `testMatch` pattern — your filename MUST match that regex. As a general guide, the pattern includes the app name as either a prefix or a suffix to distinguish specs across apps. If a config does not filter by name, use the naming convention of the most recent existing spec in `tests/` as the template.
- **Imports**: standard set —
  ```typescript
  import { test, expect } from '@playwright/test';
  import AxeBuilder from '@axe-core/playwright';
  ```
- **One `test.describe` per sub-route** of the feature, each containing exactly:
  1. `'loads at correct URL and is authenticated'`
  2. `'screenshot baseline'`
  3. `'no critical accessibility violations'`
- **`test.beforeEach`** at the top of each `describe` navigates to the sub-route URL with `{ waitUntil: 'networkidle' }`.

---

## 3. Locators — MUST follow this priority

Use the first locator type from this list that fits the element. Do not skip earlier options for later ones without a documented reason.

1. **`page.getByRole(role, { name })`** — first choice. ARIA role + accessible name. Survives styling changes, mirrors how real users and assistive tech find the element.
2. **`page.getByLabel(text)`** — form fields with a `<label>`.
3. **`page.getByPlaceholder(text)`** — inputs that have placeholders but no label (legacy code only — labels are preferred).
4. **`page.getByText(text)`** — non-interactive static text (paragraphs, captions, error messages).
5. **`page.getByAltText(text)`** — `<img>` alt attribute.
6. **`page.getByTitle(text)`** — elements with a `title` attribute.
7. **`page.getByTestId(id)`** — only when nothing above works. Requires an explicit `data-testid` on the element. Use this for elements that have no meaningful role, label, or text but must be testable.

### Locators — MUST NOT

- **CSS / XPath selectors** as the primary locator. They couple tests to DOM structure and break on refactors. Use them only when nothing above works **and** the element cannot be given a `data-testid`.
- **`text=`, `css=`, `xpath=` engine prefixes**. They are legacy. Always use the `getBy*` methods.
- **`:nth-child`, `:first-child`, positional selectors**. Position-based locators are the most brittle option. Use `.first()` / `.nth(n)` on a semantic locator only if order is intrinsic to the test.
- **Chaining `.locator('.css-class').locator('.another')`** when a single `getByRole` with `name` would do.

### When you must scope

Use `.filter({ has: ..., hasText: ... })` and `.locator(...)` chaining on **semantic** locators only — never on CSS:

```typescript
page.getByRole('row').filter({ hasText: 'Patient A' }).getByRole('button', { name: 'Edit' });
```

---

## 4. Waits — MUST NOT

- **`page.waitForTimeout(...)`** is forbidden. It introduces flakiness and slows the suite. There is always a better wait.
- **`await new Promise(r => setTimeout(r, ...))`** — same as above. Forbidden.
- **Polling with manual `while` loops** — forbidden. Use `expect.poll()` or web-first assertions.
- **Asserting on a value snapshot** like `expect(await locator.textContent()).toBe(...)`. This captures one moment in time. Use `expect(locator).toHaveText(...)` instead — it auto-retries.

## 5. Waits — MUST use

- **Web-first assertions**: every `expect(locator).*` method auto-waits until the assertion passes or times out. Always prefer these over manual waits.
- **`page.waitForResponse(...)` / `page.waitForRequest(...)`** when you need to wait for a specific API call to complete.
- **`page.waitForLoadState('networkidle')`** is allowed in `beforeEach` after `page.goto(...)` to settle a page, but do not sprinkle it inside the test body.
- **`page.waitForURL(regex)`** is allowed when the test is about navigation (e.g. clicking a button that triggers a redirect). It is NOT a substitute for `expect(page).toHaveURL(...)` in URL assertions.

---

## 6. Assertions — MUST use web-first

Every assertion against the page MUST use Playwright's auto-retrying assertions:

| Use | Not |
|---|---|
| `expect(locator).toBeVisible()` | `expect(await locator.isVisible()).toBe(true)` |
| `expect(locator).toHaveText(...)` | `expect(await locator.textContent()).toBe(...)` |
| `expect(locator).toHaveValue(...)` | `expect(await locator.inputValue()).toBe(...)` |
| `expect(locator).toHaveCount(n)` | `expect((await locator.all()).length).toBe(n)` |
| `expect(page).toHaveURL(regex)` | `expect(page.url()).toContain(...)` |
| `expect(page).toHaveTitle(regex)` | `expect(await page.title()).toMatch(regex)` |

The right-hand column captures one moment in time and fails during redirects, animations, async updates. The left-hand column auto-retries up to the configured timeout. Always use the left.

For URL assertions specifically: `expect(page).toHaveURL(...)` with a 30-second timeout handles auth redirects. Never use `page.waitForURL` for assertion purposes (it waits for an event that may already have fired) and never use `page.url().toContain(...)` (single-shot).

---

## 7. Test isolation

- Each `test()` MUST be runnable in any order, on its own. No state leaks between tests.
- Do not store data in module-level variables that change across tests.
- Setup goes in `test.beforeEach`. Cleanup goes in `test.afterEach` if needed.
- If two tests share expensive setup, use `test.describe.serial` or a fixture — never order dependence.
- Auth state is loaded via `storageState` from the setup project — do not log in inside individual tests.

---

## 8. Network and external data

- **Do not** depend on production data, real patient records, or accounts you don't own.
- For deterministic tests of dynamic content, use `page.route(url, handler)` to intercept and stub API responses.
- If a test must hit a real backend, document why — the default expectation is stubbed network for anything beyond auth.

---

## 9. Visual regression — `toHaveScreenshot`

- Filename: `<app>-<feature>-<subroute>-full.png`. Use `root` for the index sub-route.
- Always set `fullPage: true` for page-level screenshots.
- Set `maxDiffPixels: 100` (or smaller) to allow trivial anti-aliasing differences.
- **Mask dynamic regions**: timestamps, user avatars, anything that changes between runs. Use `mask: [page.getByRole('time'), ...]`.
- Wait for content to be deterministic before screenshotting. Single-page apps usually render asynchronously and show a loading placeholder until data arrives — use whatever wait helper the reference spec in `tests/` already uses for that framework. Ensure animations and skeleton states have completed before the screenshot is taken.

---

## 10. Accessibility — `AxeBuilder`

Accessibility checks are **opt-in via env var**, not default. They are slower than functional tests and developers often want to skip them during rapid iteration. The chosen mechanism is **conditional test registration** — the test is only added to the suite when the env var is set. If unset, the test does not exist in the run (no "skipped" entry, no noise).

### Required pattern

At the top of any spec file containing accessibility tests:

```typescript
const RUN_A11Y = process.env.RUN_A11Y === 'true';
```

Inside each `test.describe(...)`, wrap only the accessibility test in `if (RUN_A11Y) { ... }`:

```typescript
test.describe('Feature — sub-route', () => {
    test.beforeEach(async ({ page }) => { ... });

    test('loads at correct URL and is authenticated', async ({ page }) => { ... });
    test('screenshot baseline', async ({ page }) => { ... });

    if (RUN_A11Y) {
        test('no critical accessibility violations', async ({ page }) => {
            // ... AxeBuilder logic
        });
    }
});
```

### Behaviour

- `RUN_A11Y` unset, empty, or anything other than `"true"` → accessibility test is **not registered**. Test list shows only URL + screenshot. No skipped entries.
- `RUN_A11Y=true` → accessibility test is registered and runs.

### Why `if (RUN_A11Y)` and not `test.skip(...)`

`test.skip()` registers the test, then marks it skipped at run time. Every run prints a "skipped" line for it, which is noisy and easy to misread as a broken test. `if (RUN_A11Y)` means the test is **never registered** when the env var is off — it doesn't appear in output at all. Cleaner signal.

### To run with accessibility checks

```bash
RUN_A11Y=true npx playwright test
```

On Windows PowerShell:
```powershell
$env:RUN_A11Y='true'; npx playwright test
```

CI pipelines that should enforce accessibility set `RUN_A11Y=true` in their environment. Local hooks and developer iteration runs leave it unset.

### When the accessibility test runs, it MUST

- Use tags `['wcag2a', 'wcag2aa']`. Add `wcag21a`/`wcag21aa` if the team adopts them.
- Filter to critical impact only: `results.violations.filter(v => v.impact === 'critical')`.
- Document any `.exclude(...)` with the library/component it's a known defect in. Do not exclude without a comment.
- Enumerate violations in the failure message so the developer sees what failed without opening the trace.

---

## 11. Naming

- Test names describe behaviour, present tense, no "should": `'shows error message when password is empty'`, not `'should show error message'`.
- Use natural English, not code-style camelCase.
- `test.describe` names match the page or feature being tested.

---

## 12. Anti-patterns — MUST refuse

When generating or modifying a spec, do not write:

- `page.click('css-selector')` / `page.fill('css-selector', ...)` — use locator-first: `page.getByRole(...).click()`.
- `page.locator('//xpath')` as the primary locator.
- Hardcoded waits in milliseconds.
- Conditional assertions like `if (await locator.isVisible()) { ... }` — tests should be deterministic, not conditional.
- `try`/`catch` around assertions — let the framework report the failure properly.
- Logging via `console.log` in production specs (fine for one-off debugging, remove before commit).
- Sharing logged-in `page` across `test()` blocks — each test gets its own context via the configured `storageState`.

---

## 13. Running specs

```bash
cd DrDoctor.Playwright
npx playwright test tests/<spec-file> --config=<matching-app-config> --update-snapshots
```

`--update-snapshots` is required on the first run only. Subsequent runs compare against the saved baseline. Choose the config by matching the app discovered in section 1.

---

## 14. When auto-generation is not appropriate

Auto-generated specs cover URL-loadable pages well. Write specs **by hand** when any of these apply:

- The page needs data setup before it shows content.
- The state under test is reached via clicks (tabs that don't change the URL, modals, dropdowns).
- The test asserts business logic, not just rendering (e.g. submitting a form and verifying the result).

For these cases, follow every rule above but use `page.getByRole(...).click()` and similar to drive the UI to the right state before screenshotting or asserting.
