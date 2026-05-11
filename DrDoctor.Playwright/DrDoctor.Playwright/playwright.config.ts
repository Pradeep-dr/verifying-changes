import { defineConfig, devices } from '@playwright/test';
import * as dotenv from 'dotenv';
import * as path from 'path';

// Read credentials from .env (gitignored)
dotenv.config({ path: path.resolve(__dirname, '.env') });

// Path where the authenticated session is saved by auth.setup.ts
export const STORAGE_STATE_STAFF = path.resolve(__dirname, '.auth', 'staff.json');

export default defineConfig({
    testDir: '.',
    fullyParallel: true,
    retries: process.env.CI ? 2 : 0,
    workers: process.env.CI ? 1 : undefined,
    reporter: [['list'], ['html', { open: 'never' }]],
    timeout: 60_000,
    expect: { timeout: 10_000 },

    use: {
        baseURL: process.env.BASE_URL ?? 'https://localhost:44302',
        ignoreHTTPSErrors: true,
        trace: 'on',
        screenshot: 'on',
        video: 'retain-on-failure',
    },

        projects: [
        {
            name: 'setup',
            testMatch: /auth\.setup\.ts/,
        },
        {
            name: 'chromium',
            testMatch: /.*\.spec\.ts/,
            dependencies: ['setup'],
            use: {
                ...devices['Desktop Chrome'],
                storageState: STORAGE_STATE_STAFF,
            },
        },
    ],
});
