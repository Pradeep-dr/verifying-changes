import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const PROVIDER_ID = 1;

const routes = [
    { name: 'root',         url: `/waitinglists/${PROVIDER_ID}`,              title: 'Waiting Lists' },
    { name: 'config',       url: `/waitinglists/${PROVIDER_ID}/config`,       title: 'Configuration' },
    { name: 'entries',      url: `/waitinglists/${PROVIDER_ID}/entries`,      title: 'Entries' },
    { name: 'patientlists', url: `/waitinglists/${PROVIDER_ID}/patientlists`, title: 'Patient Lists' },
];

for (const route of routes) {
    test.describe(`Waiting Lists — ${route.title}`, () => {

        test('loads at correct URL', async ({ page }) => {
            await page.goto(route.url);
            await expect(page).toHaveURL(route.url);
        });

        test('screenshot baseline', async ({ page }) => {
            await page.goto(route.url);
            await page.waitForLoadState('networkidle');
            await expect(page).toHaveScreenshot(`waitinglists-${route.name}-full.png`, {
                fullPage: true,
            });
        });

        test('accessibility — no WCAG violations', async ({ page }) => {
            await page.goto(route.url);
            const results = await new AxeBuilder({ page }).analyze();
            expect(results.violations).toEqual([]);
        });

    });
}
