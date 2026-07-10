import { test, expect } from "../../fixtures/auth.fixture";
import { loginAs } from "../../helpers/auth";
import { alice, SEED_PASSWORD } from "../../fixtures/seed-data";

test.describe("Session bootstrap", () => {
  test("session persists across page reload", async ({
    authenticatedPage: page,
  }) => {
    await expect(
      page.getByRole("heading", { level: 1 }),
    ).toContainText(`Welcome back, ${alice.displayName}`);

    await page.reload();

    await expect(
      page.getByRole("heading", { level: 1 }),
    ).toContainText(`Welcome back, ${alice.displayName}`);
  });

  test("unauthenticated user is redirected to login", async ({ page }) => {
    await page.goto("/");

    await page.waitForURL(/\/login/);
  });

  test("protected routes redirect to login with return path", async ({
    page,
  }) => {
    await page.goto("/organizer/events");

    await page.waitForURL(/\/login/);
    await expect(page.locator("#login-email")).toBeVisible();
  });
});
