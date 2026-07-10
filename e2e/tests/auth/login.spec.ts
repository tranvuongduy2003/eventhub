import { test, expect } from "../../fixtures/auth.fixture";
import { LoginPage } from "../../pages/login.page";
import { alice, SEED_PASSWORD } from "../../fixtures/seed-data";

test.describe("Organizer login", () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test("seed user logs in and sees dashboard", async ({ page }) => {
    await loginPage.login(alice.email, SEED_PASSWORD);

    await page.waitForURL("/");
    await expect(
      page.getByRole("heading", { level: 1 }),
    ).toContainText(`Welcome back, ${alice.displayName}`);
  });

  test("invalid credentials show error message", async () => {
    await loginPage.login(alice.email, "WrongPassword123!");

    await loginPage.expectRootError("Email or password is incorrect.");
  });

  test("non-existent email shows same error (no enumeration)", async () => {
    await loginPage.login("nobody@e2e.dev", "Whatever123!");

    await loginPage.expectRootError("Email or password is incorrect.");
  });

  test("empty form shows client validation", async ({ page }) => {
    await loginPage.submitButton.click();

    await expect(page).toHaveURL("/login");
  });

  test("redirects to originally requested page after login", async ({
    page,
  }) => {
    await page.goto("/organizer/events");
    await page.waitForURL(/\/login/);

    await loginPage.login(alice.email, SEED_PASSWORD);

    await page.waitForURL(/\/(organizer\/events|$)/);
  });
});
