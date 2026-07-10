import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.E2E_BASE_URL ?? "https://localhost:5000";

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [["html", { open: "never" }]],
  webServer: {
    command: "dotnet run --project ../src/AppHost/EventHub.AppHost.csproj",
    url: baseURL,
    reuseExistingServer: true,
    timeout: 300_000,
    ignoreHTTPSErrors: true,
  },

  use: {
    baseURL,
    ignoreHTTPSErrors: true, // Aspire self-signed dev cert
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
