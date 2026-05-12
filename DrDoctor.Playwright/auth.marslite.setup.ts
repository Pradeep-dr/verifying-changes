import { test as setup, expect } from '@playwright/test';
import { STORAGE_STATE_MARSLITE } from './playwright.marslite.config';

// MarsLite uses a local cookie auth — a Razor form at /auth/login that
// validates against the seeded SQLite database. Credentials come from .env
// so the seed values aren't hardcoded in source.
setup.setTimeout(60_000);

setup('authenticate as marslite staff', async ({ page }) => {
    const username = process.env.MARSLITE_USERNAME;
    const password = process.env.MARSLITE_PASSWORD;

    if (!username || !password) {
        throw new Error(
            'MARSLITE_USERNAME and MARSLITE_PASSWORD must be set in DrDoctor.Playwright/.env',
        );
    }

    await page.goto('/auth/login');

    await page.getByLabel('Email').fill(username);
    await page.getByLabel('Password').fill(password);
    await page.getByRole('button', { name: 'Sign in' }).click();

    // Web-first assertion auto-retries until the redirect off /auth/login completes.
    // If credentials are wrong, the page stays on /auth/login and this fails clearly.
    await expect(page).not.toHaveURL(/\/auth\/login/);

    // Ensure the post-login request has settled before snapshotting storage
    // state — otherwise the Set-Cookie response may not be flushed into the
    // context yet on slow cold-starts.
    await page.waitForLoadState('networkidle');

    await page.context().storageState({ path: STORAGE_STATE_MARSLITE });
});
