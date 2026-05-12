import { defineConfig, devices } from '@playwright/test';
import * as dotenv from 'dotenv';
import * as path from 'path';

dotenv.config({ path: path.resolve(__dirname, '.env') });

export const STORAGE_STATE_PATIENT = path.resolve(__dirname, '.auth', 'patient.json');

export default defineConfig({
    testDir: '.',
    fullyParallel: true,
    retries: process.env.CI ? 2 : 0,
    workers: process.env.CI ? 1 : undefined,
    reporter: [['list'], ['html', { open: 'never' }]],
    timeout: 60_000,
    expect: { timeout: 10_000 },

    use: {
        // PatientPortal runs on 7108 (dotnet run) or 44397 (IIS Express)
        baseURL: process.env.PATIENT_PORTAL_URL ?? 'https://localhost:7108',
        ignoreHTTPSErrors: true,
        trace: 'on',
        screenshot: 'on',
        video: 'retain-on-failure',
    },

    projects: [
        {
            name: 'setup',
            testMatch: /auth\.patientportal\.setup\.ts/,
        },
        {
            name: 'chromium',
            testMatch: /.*\.patientportal\.spec\.ts/,
            dependencies: ['setup'],
            use: {
                ...devices['Desktop Chrome'],
                storageState: STORAGE_STATE_PATIENT,
            },
        },
    ],
});
