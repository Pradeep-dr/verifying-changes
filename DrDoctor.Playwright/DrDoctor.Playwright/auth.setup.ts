import { test as setup, expect } from '@playwright/test';
import { STORAGE_STATE_STAFF } from './playwright.config';

// Auth0 + IIS Express cold-start can be slow — give it 3 minutes
setup.setTimeout(180_000);

setup('authenticate as staff user', async ({ page }) => {
    const username = process.env.STAFF_USERNAME;
    const password = process.env.STAFF_PASSWORD;

    if (!username || !password) {
        throw new Error(
            'STAFF_USERNAME and STAFF_PASSWORD must be set in DrDoctor.Playwright/.env',
        );
    }

    // 1. Hit /login — Mars redirects to Auth0
    await page.goto('/login');

    // 2. Wait for the Auth0 hosted login page to load
    await page.waitForURL(/manage\.drdoctor\.dev\/u\/login/, { timeout: 30_000 });

    // 3. Fill in credentials
    const usernameField = page.locator('input[name="username"], input[type="email"]').first();
    const passwordField = page.locator('input[name="password"], input[type="password"]').first();
    await usernameField.waitFor({ state: 'visible', timeout: 15_000 });
    await usernameField.fill(username);
    await passwordField.fill(password);

        // 4. Submit
    await page.locator('button[type="submit"]').first().click();

    // 5. Wait until we're back on any Mars URL (callback counts as Mars)
    await page.waitForURL(/localhost:44302/, { timeout: 60_000 });

    // 6. Force-navigate to home — Mars completes the OAuth round-trip here
    await page.goto('/');
    await page.waitForLoadState('networkidle', { timeout: 30_000 });

    // 7. Visit a real authenticated page so Mars consumes / clears any
    //    transient OWIN OpenIdConnect state cookies. Without this step the
    //    saved storageState contains stale OAuth state that causes a
    //    "Auth0.Core.Exceptions.ErrorApiException: Unauthorized" 500 error
    //    on the very first navigation in subsequent test runs (Mars retries
    //    the auth-code exchange and Auth0 rejects the already-consumed code).
    await page.goto('/waitinglists/-1');
    await page.waitForLoadState('networkidle', { timeout: 30_000 });

    // 8. Confirm we're authenticated (not bounced back to /login)
    await expect(page).not.toHaveURL(/\/login(\?|$)/);

    // 9. Save the cleaned-up authenticated session for all specs to reuse
    await page.context().storageState({ path: STORAGE_STATE_STAFF });
});
