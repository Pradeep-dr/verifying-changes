import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

// Set MARS_PROVIDER_ID in .env — must be a provider the test user has access to
const PROVIDER_ID = process.env.MARS_PROVIDER_ID ?? "1";

const routes = [
  { name: "overview", path: `/waitinglists/${PROVIDER_ID}` },
  { name: "config", path: `/waitinglists/${PROVIDER_ID}/config` },
  { name: "entries", path: `/waitinglists/${PROVIDER_ID}/entries` },
  { name: "patient-lists", path: `/waitinglists/${PROVIDER_ID}/patientlists` },
];

// Wait for the AngularJS ng-view to finish rendering and API calls to settle
async function waitForAngular(page: import("@playwright/test").Page) {
  await page.waitForLoadState("networkidle");
  await expect(page.locator('[data-record="true"]')).not.toContainText(
    "We're currently loading",
    { timeout: 15_000 },
  );
}

for (const route of routes) {
  test.describe(`Mars — Waiting Lists — ${route.name}`, () => {
    test.beforeEach(async ({ page }) => {
      await page.goto(route.path);
      await waitForAngular(page);
    });

    test("loads at correct URL and is authenticated", async ({ page }) => {
      // Should not be redirected to login
      await expect(page).not.toHaveURL(/\/login/);
      await expect(page).toHaveURL(new RegExp(route.path));
    });

    test("page title is Waiting Lists", async ({ page }) => {
      await expect(page).toHaveTitle(/Waiting lists/i);
    });

    test("screenshot baseline", async ({ page }) => {
      await expect(page).toHaveScreenshot(`${route.name}.png`, {
        fullPage: true,
        // Real data changes — allow minor pixel drift from dynamic content
        maxDiffPixelRatio: 0.02,
      });
    });

    test("accessibility — no WCAG violations", async ({ page }) => {
      const results = await new AxeBuilder({ page })
        .withTags(["wcag2a", "wcag2aa", "wcag21aa"])
        .analyze();
      expect(results.violations).toEqual([]);
    });
  });
}
