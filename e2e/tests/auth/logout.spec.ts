import { test, expect } from "../../fixtures/auth.fixture";

const apiUrl = process.env.E2E_API_URL ?? "https://localhost:8000";

test.describe("Logout", () => {
  test("logout clears session and redirects to login", async ({
    authenticatedPage: page,
  }) => {
    await expect(
      page.getByRole("heading", { level: 1 }),
    ).toContainText("Welcome back");

    const response = await page.request.post(`${apiUrl}/api/auth/logout`);
    expect(response.status()).toBe(204);

    await page.goto("/");
    await page.waitForURL(/\/login/);
  });

  test("session cookie is deleted after logout", async ({
    authenticatedPage: page,
  }) => {
    await page.request.post(`${apiUrl}/api/auth/logout`);

    const meResponse = await page.request.get(`${apiUrl}/api/auth/me`);
    expect(meResponse.status()).toBe(401);
  });
});
