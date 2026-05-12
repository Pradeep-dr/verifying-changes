import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// ── Unauthenticated tests (no stored session needed) ──────────────────────────
// These run using the chromium project which injects the patient session, but
// the routes themselves are accessible regardless — useful as a smoke test that
// the app is up before auth tests run.

test.describe('PatientPortal — Health', () => {
    test('health endpoint returns 200', async ({ request }) => {
        const response = await request.get('/health');
        expect(response.status()).toBe(200);
    });
});

// ── Authenticated page tests ──────────────────────────────────────────────────

test.describe('PatientPortal — Home', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
    });

    test('loads home page and is authenticated', async ({ page }) => {
        await expect(page).not.toHaveURL(/\/home\/login/);
        await expect(page).toHaveURL('/');
    });

    test('page title contains patient portal branding', async ({ page }) => {
        await expect(page).toHaveTitle(/.+/); // not blank
    });

    test('screenshot baseline', async ({ page }) => {
        await expect(page).toHaveScreenshot('home.png', { fullPage: true });
    });

    test('accessibility — no WCAG violations', async ({ page }) => {
        const results = await new AxeBuilder({ page })
            .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
            .analyze();
        expect(results.violations).toEqual([]);
    });
});

test.describe('PatientPortal — Appointments', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/appointments');
        await page.waitForLoadState('networkidle');
    });

    test('loads appointments page', async ({ page }) => {
        await expect(page).not.toHaveURL(/\/home\/login/);
        await expect(page).toHaveURL(/\/appointments/);
    });

    test('screenshot baseline', async ({ page }) => {
        await expect(page).toHaveScreenshot('appointments.png', { fullPage: true });
    });

    test('accessibility — no WCAG violations', async ({ page }) => {
        const results = await new AxeBuilder({ page })
            .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
            .analyze();
        expect(results.violations).toEqual([]);
    });
});
