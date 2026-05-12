import { defineConfig, devices } from "@playwright/test";

// No auth needed — MarsLite has no login
export default defineConfig({
  testDir: ".",
  fullyParallel: true,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [["list"], ["html", { open: "never" }]],
  timeout: 60_000,
  expect: { timeout: 10_000 },

  use: {
    baseURL: "http://localhost:5185",
    trace: "on",
    screenshot: "on",
    video: "retain-on-failure",
  },

  projects: [
    {
      name: "chromium",
      testMatch: /.*\.spec\.ts/,
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
