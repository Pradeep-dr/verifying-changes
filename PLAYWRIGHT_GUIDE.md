# DrDoctor Playwright — Developer Guide

## What This Is

Playwright automatically tests Mars Staff Portal pages. Every time you commit code that changes a UI page, the tests run automatically and block the commit if something looks wrong — a broken layout, a missing element, or a visual regression.

---

## Prerequisites

- Node.js 18 or later — https://nodejs.org
- Git
- Access to the DrDoctor repo
- Your DrDoctor staff login credentials

---

## Part 1 — First Time Setup

### Step 1 — Place the folders

You will receive two folders. Place them in the repo root (`DrDoctor/`) — the same level as `src/` and `DrDoctor.sln`:

```
DrDoctor/
├── .githooks/            ← place here
├── DrDoctor.Playwright/  ← place here
├── src/
├── DrDoctor.sln
```

### Step 2 — Install packages

Open a terminal inside `DrDoctor.Playwright/`:

```bash
cd DrDoctor.Playwright
npm install
npx playwright install chromium
```

### Step 3 — Create your .env file

Inside `DrDoctor.Playwright/`, create a file called `.env` (no extension) with the following content:

```
BASE_URL=https://localhost:44302
STAFF_USERNAME=your.email@drdoctor.co.uk
STAFF_PASSWORD=your_password
```

> This file is gitignored — your credentials will never be committed to the repo.

### Step 4 — Activate the git hook

Run this **once** in the repo root (`DrDoctor/`):

```bash
git config core.hooksPath .githooks
```

Verify it worked:

```bash
git config core.hooksPath
```

Should print: `.githooks`

### Step 5 — Generate your auth session

Start Mars locally (press **Ctrl+F5** in Visual Studio), then run:

```bash
cd DrDoctor.Playwright
npm run auth
```

This logs in to Mars using your credentials and saves the session. You only need to re-run this if your session expires or you change your password.

---

## Part 2 — Writing a Test for a New Page

Do this **once per module** — whenever you create a new Mars page in the backend and frontend.

### What auto-generated tests can and cannot do

**Works well for:** top-level pages that load standalone via a URL change (e.g. `/waitinglists/-1/`, `/waitinglists/-1/patientLists`). These produce reliable URL + screenshot + accessibility tests.

**Does NOT work for:**

- Pages that need data setup before they show content (e.g. an Entries page that is empty until a waiting list is created)
- Tabs that do not change the URL (e.g. Settings / Clinic / Webpage tabs inside Configuration)
- Modals, dropdowns, or any interactive flow that requires clicking through the UI

For these, write the test **manually** — use Playwright's `page.click()`, `page.fill()` etc. to drive the UI to the right state before screenshotting.

### Important — How Mars pages actually work

Before writing tests, you need to understand how a Mars page is structured. A single Mars "page" is rarely one URL — it is usually one Nancy module + several AngularJS sub-routes.

**Real example — the Waiting Lists page:**

| Layer    | File                                                        | What it does                                                          |
| -------- | ----------------------------------------------------------- | --------------------------------------------------------------------- |
| Backend  | `src/Mars/Mars/Modules/WaitingListsModule.cs`               | Single Nancy route: `Get["/{providerId:int}"]` returns one shell page |
| Frontend | `src/Mars/Mars/Scripts/app/waitingLists/waitingListsApp.js` | AngularJS `$routeProvider` defines 4 sub-routes inside that shell     |

Inside `waitingListsApp.js` (line 1010) you will find:

```javascript
.config(['$routeProvider', '$locationProvider', function ($routeProvider, $locationProvider) {
    $routeProvider
        .when('/',             { templateUrl: '/Scripts/templates/waitingLists/root.html',         controller: 'RootController' })
        .when('/config',       { templateUrl: '/Scripts/templates/waitingLists/configuration.html', controller: 'ConfigurationController' })
        .when('/entries',      { templateUrl: '/Scripts/templates/waitingLists/entries.html',       controller: 'EntriesController' })
        .when('/patientLists', { templateUrl: '/Scripts/templates/waitingLists/patientLists.html',  controller: 'PatientListsController' });
}]);
```

This means the Waiting Lists "page" has **4 sub-pages** that all need testing:

| URL                             | What it shows                     |
| ------------------------------- | --------------------------------- |
| `/waitinglists/-1/`             | Root view (list of waiting lists) |
| `/waitinglists/-1/config`       | Configuration page                |
| `/waitinglists/-1/entries`      | Entries page                      |
| `/waitinglists/-1/patientLists` | Patient lists page                |

> **The pattern in Mars:** the Nancy module returns one shell, then AngularJS routes inside it (via `$routeProvider`) drive all the sub-pages. You will see the same pattern in `clinic-setup`, `broadcast`, `assessments` and most other Mars modules.

### Step 1 — Find all the sub-routes for your page

Before writing the test, locate the AngularJS app file for your module — it is in:

```
src/Mars/Mars/Scripts/app/<moduleName>/<moduleName>App.js
```

Search for `$routeProvider` in that file. The `.when('...')` entries are your sub-routes.

### Step 2 — Write the test using Claude Code

Open Claude Code in the repo root and type:

```
/project:new-playwright-test /waitinglists/-1/
```

Claude will:

1. Read `WaitingListsModule.cs` to confirm the Nancy route
2. Read `waitingListsApp.js` to discover all `$routeProvider` sub-routes
3. Create `DrDoctor.Playwright/tests/waitinglists.spec.ts` with the three standard tests for each sub-route

For Waiting Lists you will get **12 tests in total** (4 sub-pages × 3 standard tests):

| Sub-page                        | Tests written                        |
| ------------------------------- | ------------------------------------ |
| `/waitinglists/-1/`             | URL check, screenshot, accessibility |
| `/waitinglists/-1/config`       | URL check, screenshot, accessibility |
| `/waitinglists/-1/entries`      | URL check, screenshot, accessibility |
| `/waitinglists/-1/patientLists` | URL check, screenshot, accessibility |

**The three standard tests per sub-page:**

- **URL check** — confirms the page loads at the correct URL
- **Screenshot** — takes a full-page screenshot as a visual baseline
- **Accessibility** — scans for WCAG 2.1 AA violations

> You do not need to list the sub-routes yourself or write any test code. Claude reads the code and figures it all out.

### Step 3 — Create the screenshot baselines

Start Mars, make sure your auth session is valid, then run:

```bash
cd DrDoctor.Playwright
npx playwright test tests/waitinglists.spec.ts --update-snapshots
```

Playwright will visit each sub-route, take a full-page screenshot and save it as a baseline. You will see:

```
4 snapshots written
```

> This is not a failure — it means the 4 baselines (one per sub-page) were created successfully. From now on, every test run will compare each sub-page against its saved screenshot.

Verify the baseline PNGs were created in:

```
DrDoctor.Playwright/tests/waitinglists.spec.ts-snapshots/
    waitinglists-root-full.png
    waitinglists-patientLists-full.png
    waitinglists-config-full.png       (may be empty — see note below)
    waitinglists-entries-full.png      (may be empty — see note below)
```

> **Open every baseline PNG and check it visually before committing.** Some sub-routes (like Configuration and Entries on Waiting Lists) require data setup or user interaction first — without that, the URL silently redirects back to the root, and the baseline ends up being a screenshot of the wrong page. If you see this, either:
>
> - Delete that sub-route's `test.describe` block from the spec, or
> - Rewrite it manually with the clicks needed to reach the right page (see the limitation note at the top of Part 2).

### Step 4 — Add the mapping in .githooks/pre-commit

The pre-commit hook needs to know which source files trigger which test. Open `.githooks/pre-commit` in the repo root and find this section:

```bash
# -----------------------------------------------------------------
# ADD YOUR PAGE MAPPINGS HERE
# -----------------------------------------------------------------
```

Add an entry for your new page **above** the `*)` catch-all line.

**Real example — Waiting Lists (covers both backend and frontend changes):**

```bash
src/Mars/Mars/Modules/WaitingListsModule.cs|src/Mars/Mars/Views/WaitingLists/*|src/Mars/Mars/Scripts/app/waitingLists/*|src/Mars/Mars/Scripts/templates/waitingLists/*)
    SPECS_TO_RUN="$SPECS_TO_RUN tests/waitinglists.spec.ts"
    ;;
```

**How to fill in each path:**

| Path                                             | What it matches                                                | Where to find it                   |
| ------------------------------------------------ | -------------------------------------------------------------- | ---------------------------------- |
| `src/Mars/Mars/Modules/WaitingListsModule.cs`    | The Nancy module                                               | `src/Mars/Mars/Modules/`           |
| `src/Mars/Mars/Views/WaitingLists/*`             | The Nancy shell view (.cshtml)                                 | `src/Mars/Mars/Views/`             |
| `src/Mars/Mars/Scripts/app/waitingLists/*`       | AngularJS app + controllers + services                         | `src/Mars/Mars/Scripts/app/`       |
| `src/Mars/Mars/Scripts/templates/waitingLists/*` | AngularJS HTML templates (root, config, entries, patientLists) | `src/Mars/Mars/Scripts/templates/` |
| `tests/waitinglists.spec.ts`                     | The spec file Claude created in Step 2                         | `DrDoctor.Playwright/tests/`       |

> **Why so many paths?** A change to any of these layers can break the page. The hook needs to know about all of them — backend module, Nancy view, AngularJS app, AngularJS templates.

**Real example — Clinic Setup page (different folder pattern):**

```bash
src/Mars/Mars/Modules/ClinicSetup/*|src/Mars/Mars/Views/ClinicSetup/*|src/Mars/Mars/Scripts/app/clinic-setup/*)
    SPECS_TO_RUN="$SPECS_TO_RUN tests/clinic-setup.spec.ts"
    ;;
```

> Use `/*` at the end when the page has multiple files inside a folder. Use a single filename like `WaitingListsModule.cs` when there is only one module file.

### Step 5 — Commit everything

```bash
git add DrDoctor.Playwright/tests/waitinglists.spec.ts
git add DrDoctor.Playwright/tests/waitinglists.spec.ts-snapshots/
git add .githooks/pre-commit
git commit -m "Add Playwright tests for Waiting Lists page"
```

> The hook itself will not run on this commit because none of the staged files match a mapped pattern yet — you are _adding_ the mapping, not changing a mapped page.

---

## Part 3 — Day to Day Workflow

### How the git hook decides what to run

The hook runs automatically every time you do `git commit`.

- If your staged files **do not** touch any mapped UI pages — the hook skips and the commit goes through instantly.
- If your staged files **do** touch a mapped UI page — the relevant Playwright tests run before the commit is allowed.

### Scenario A — Initial commit (adding the new tests)

After you generated the spec and baselines for Waiting Lists, your first commit looks like this:

```bash
git add DrDoctor.Playwright/tests/waitinglists.spec.ts
git add DrDoctor.Playwright/tests/waitinglists.spec.ts-snapshots/
git add .githooks/pre-commit
git commit -m "Add Playwright tests for Waiting Lists page"
```

Hook output:

```
[pre-commit] -----------------------------------------------------------
[pre-commit] DrDoctor automated UI verification
[pre-commit] -----------------------------------------------------------
[pre-commit] No staged UI changes. Skipping Playwright.
```

> No Mars source files were staged — only the test spec, baselines, and the hook file itself. The hook correctly skips. Commit succeeds.

### Scenario B — You change a UI file that's now mapped

A week later you tweak a label in the Waiting Lists HTML template:

```html
<!-- edit: src/Mars/Mars/Scripts/templates/waitingLists/root.html -->
-
<h2>Waiting Lists</h2>
+
<h2>All Waiting Lists</h2>
```

```bash
git add src/Mars/Mars/Scripts/templates/waitingLists/root.html
git commit -m "Rename heading on Waiting Lists root page"
```

Hook output if the test **passes** (e.g. just a CSS tidy-up that doesn't change the visual):

```
[pre-commit] Detected staged UI files:
             src/Mars/Mars/Scripts/templates/waitingLists/root.html
[pre-commit] Running:
             - tests/waitinglists.spec.ts
[pre-commit]
... (Playwright runs)
[pre-commit] OK  All tests passed. Proceeding with commit.
```

Hook output if the test **fails** (which it will, because the heading text changed):

```
[pre-commit] Detected staged UI files:
             src/Mars/Mars/Scripts/templates/waitingLists/root.html
[pre-commit] Running:
             - tests/waitinglists.spec.ts
[pre-commit]
... (Playwright runs)
[pre-commit] -----------------------------------------------------------
[pre-commit] X  Tests failed. Commit blocked.
[pre-commit]
[pre-commit]   View report  : cd DrDoctor.Playwright && npx playwright show-report
[pre-commit]   Emergency    : git commit --no-verify
[pre-commit] -----------------------------------------------------------
```

The commit is blocked because the screenshot no longer matches the baseline (the heading text is different).

### Scenario C — Confirm the change and update the baseline

Open the report to see what changed:

```bash
cd DrDoctor.Playwright
npm run report
```

The report shows a side-by-side diff: **expected** (old baseline) vs **actual** (new render) with the differences highlighted in red.

If the change is intentional (you really did want "All Waiting Lists"), update the baseline:

```bash
cd DrDoctor.Playwright
npx playwright test tests/waitinglists.spec.ts --update-snapshots
```

Then stage the new baseline PNG along with your code change and commit:

```bash
git add src/Mars/Mars/Scripts/templates/waitingLists/root.html
git add DrDoctor.Playwright/tests/waitinglists.spec.ts-snapshots/waitinglists-root-full-chromium-win32.png
git commit -m "Rename heading on Waiting Lists root page"
```

Hook output:

```
[pre-commit] Detected staged UI files:
             src/Mars/Mars/Scripts/templates/waitingLists/root.html
[pre-commit] Running:
             - tests/waitinglists.spec.ts
[pre-commit] OK  All tests passed. Proceeding with commit.
```

> The new screenshot now matches the new baseline. Commit succeeds.

### Scenario D — Tests caught a real bug

You changed some CSS in a different page and the Waiting Lists screenshot test starts failing — you didn't _mean_ to affect Waiting Lists. Fix the CSS so Waiting Lists looks unchanged, then commit again. The hook will re-run the test and let the commit through.

### Scenario E — Emergency hotfix only

> If you absolutely have to commit without running tests (e.g. production is on fire), use:

```bash
git commit --no-verify
```

This bypasses the hook. Only use in genuine emergencies — the tests will still run in CI and may block the pull request.

### Refreshing your auth session

If tests fail with a server error or the Auth0 login page appears, your session has expired. Restart Mars and run:

```bash
cd DrDoctor.Playwright
npm run auth
```

---

## Part 4 — Useful Commands

| Command                                  | What it does                       |
| ---------------------------------------- | ---------------------------------- |
| `npm run auth`                           | Regenerate the auth session        |
| `npm test`                               | Run all specs                      |
| `npx playwright test tests/page.spec.ts` | Run one spec                       |
| `npx playwright test --update-snapshots` | Update all screenshot baselines    |
| `npm run report`                         | Open the last HTML test report     |
| `npm run test:headed`                    | Run tests with the browser visible |
| `npm run test:ui`                        | Open Playwright interactive UI     |

---

## Part 5 — Troubleshooting

| Problem                                                    | Fix                                                                               |
| ---------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `npm run auth` fails                                       | Make sure Mars is running (Ctrl+F5 in Visual Studio)                              |
| Tests show Auth0 login page                                | Session expired — run `npm run auth`                                              |
| Screenshot test fails after intentional UI change          | Run `--update-snapshots` and commit the new PNG                                   |
| `git config core.hooksPath` shows nothing                  | Run `git config core.hooksPath .githooks` again                                   |
| Visual Studio breaks on `RouteExecutionEarlyExitException` | Right-click the exception → tick "Don't break when this exception type is thrown" |
| Test fails with server error page                          | Delete `.auth/`, restart Mars with Ctrl+F5, run `npm run auth` again              |
