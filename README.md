# verifyChanges — DrDoctor POC + Playwright harness

Three projects in this repo:

| Project | What it is | Tech |
|---|---|---|
| **MarsLite** | Staff-portal POC mirroring real Mars | .NET Framework 4.8 · Nancy 1.4 · OWIN · Razor · AngularJS 1.8 · SQLite + Dapper |
| **PatientPortalLite** | Patient-portal POC mirroring real PatientPortal | .NET 9 · ASP.NET Core MVC · EF Core · SQLite |
| **DrDoctor.Playwright** | UI test harness for Mars / MarsLite / PatientPortal | Playwright · TypeScript · axe-core |

Both Lite apps are self-contained — they create and seed their SQLite database on first run.

---

## Prerequisites

- **Windows** (MarsLite needs .NET Framework, only available on Windows)
- **.NET SDK 9** — `dotnet --list-sdks` should show `9.0.x`
- **.NET Framework 4.8 Developer Pack** — for MarsLite (default on Windows 11)
- **Node.js 18+** — for the Playwright tests
- A modern browser

---

## MarsLite (staff portal)

```bash
cd MarsLite/src/MarsLite.Web
dotnet run
```

Open <http://localhost:5185>.

**Demo credentials:** `staff@drdoctor.dev` / `password123`

Routes:
- `/` — Dashboard
- `/waitinglists/1` — Waiting Lists shell (Razor + AngularJS sub-routes: Overview / Configuration / Entries / Patient Lists)
- `/waitinglists/1/data/*` — JSON endpoints feeding the AngularJS app
- `/appointments` and `/appointments/{guid}` — GUID-routed stub

Override the port:
```bash
MARSLITE_URL=http://localhost:5190 dotnet run
```

---

## PatientPortalLite (patient portal)

```bash
cd PatientPortalLite/src/PatientPortalLite.Web
dotnet run
```

Open <http://localhost:5219>.

**Demo sign-in (two steps):**

| Step | Field | Value |
|---|---|---|
| 1 — demographics | Last name | `Johnson` |
|  | Date of birth | `1985-04-12` |
|  | Postcode | `SW1A 1AA` |
| 2 — one-time code | Code | `123456` |

Routes:
- `/` — Home (notice + booking banner + upcoming appointments)
- `/appointments` — appointments list
- `/appointments/{guid}` — appointment detail (Appointment Info / Directions / Clinic Info tabs)
- `/letters` and `/letters/{guid}`

To run with HTTPS too:
```bash
dotnet run --launch-profile https     # http://localhost:5219 + https://localhost:7269
```

---

## Playwright tests

```bash
cd DrDoctor.Playwright/DrDoctor.Playwright
npm install
npx playwright install chromium      # first time only
```

| Target | Command | Needs |
|---|---|---|
| MarsLite | `npm run test:marslite` | MarsLite running on `http://localhost:5185` |
| Real Mars | `npm run test:mars` | Mars running on `https://localhost:44302` + `.env` with `STAFF_USERNAME` / `STAFF_PASSWORD` |
| PatientPortalLite | `npm run test:pp` | PatientPortalLite running + `PATIENT_PORTAL_URL` / `PATIENT_PORTAL_TEST_PATIENT_ID` set |

Each spec checks: URL loads, full-page screenshot baseline, WCAG 2.1 AA accessibility (axe-core).

First test run creates the screenshot baselines. Subsequent runs compare against them — any pixel diff fails the test and shows up in the HTML report:
```bash
npm run report                        # open the last report
```

`.env` lives at `DrDoctor.Playwright/DrDoctor.Playwright/.env` and is gitignored.

---

## How everything fits together

```
┌─────────────────┐      ┌──────────────────────────┐      ┌──────────────────┐
│  MarsLite       │◄─────│  DrDoctor.Playwright     │─────►│  Real Mars       │
│  :5185          │      │  (specs + configs)       │      │  :44302 + Auth0  │
└─────────────────┘      └────────────┬─────────────┘      └──────────────────┘
                                      │
                                      ▼
                         ┌──────────────────────────┐
                         │  PatientPortalLite       │
                         │  :5219                   │
                         └──────────────────────────┘
```

Each Lite app can be developed and tested against without touching the real services.

---

## Resetting state

Each Lite app creates its SQLite database next to the binary:
- MarsLite → `MarsLite/src/MarsLite.Web/bin/Debug/net48/marslite.db`
- PatientPortalLite → `PatientPortalLite/src/PatientPortalLite.Web/bin/Debug/net9.0/patientportal.db`

Delete the `.db` file and restart the app to re-seed from scratch.

To wipe Playwright auth state:
```bash
rm -rf DrDoctor.Playwright/DrDoctor.Playwright/.auth
```
