import { test as setup, expect } from '@playwright/test';
import { STORAGE_STATE_PATIENT } from './playwright.patientportal.config';

// PatientPortal uses OIDC (DrDoctor IdentityServer) — demographics + OTC flow.
// For Playwright we bypass that using a dev-only endpoint in PatientPortal that
// signs in a test patient directly (see DevAuthController.cs in PatientPortal/Web).
// Requires PATIENT_PORTAL_TEST_PATIENT_ID in .env — must match a real patient in the dev DB.
setup.setTimeout(60_000);

setup('authenticate as test patient', async ({ page }) => {
    const patientId = process.env.PATIENT_PORTAL_TEST_PATIENT_ID;

    if (!patientId) {
        throw new Error(
            'PATIENT_PORTAL_TEST_PATIENT_ID must be set in DrDoctor.Playwright/.env\n' +
            'Value should be a patient GUID that exists in the dev PatientPortal database.',
        );
    }

    // Hit the dev-only sign-in endpoint — creates a cookie session and redirects to /
    await page.goto(`/dev-auth?patientId=${patientId}`);

    // If the endpoint is missing (e.g. app is running in Production mode), fail clearly
    await expect(page, 'DevAuthController not found — is PatientPortal running in Development?')
        .not.toHaveURL(/\/dev-auth/);

    // Wait for the home page to settle
    await page.waitForLoadState('networkidle');

    // Confirm we landed on an authenticated page, not the login redirect
    await expect(page).not.toHaveURL(/\/home\/login/);

    await page.context().storageState({ path: STORAGE_STATE_PATIENT });
});
