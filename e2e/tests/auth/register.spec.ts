import { test, expect } from "../../fixtures/auth.fixture";
import { RegisterPage } from "../../pages/register.page";
import { alice } from "../../fixtures/seed-data";

test.describe("Organizer registration", () => {
  let registerPage: RegisterPage;

  test.beforeEach(async ({ page }) => {
    registerPage = new RegisterPage(page);
    await registerPage.goto();
  });

  test("new organizer registers and lands on home", async ({ page }) => {
    const unique = Date.now();
    await registerPage.register({
      displayName: `Test User ${unique}`,
      email: `testuser${unique}@e2e.dev`,
      password: "Str0ng!Pass",
    });

    await page.waitForURL("/");
    await expect(
      page.getByRole("heading", { level: 1 }),
    ).toContainText("Welcome back");
  });

  test("duplicate email shows field error", async () => {
    await registerPage.register({
      displayName: "Duplicate Test",
      email: alice.email,
      password: "Str0ng!Pass",
    });

    await registerPage.expectRootError(
      "An account with this email already exists.",
    );
  });

  test("weak password shows validation error", async ({ page }) => {
    await registerPage.register({
      displayName: "Weak Pass User",
      email: `weakpass${Date.now()}@e2e.dev`,
      password: "123",
    });

    await expect(page.locator("#register-password").locator("..")).toContainText(
      /character/i,
    );
  });

  test("mismatched passwords show error", async ({ page }) => {
    await registerPage.register({
      displayName: "Mismatch User",
      email: `mismatch${Date.now()}@e2e.dev`,
      password: "Str0ng!Pass",
      confirmPassword: "Different!Pass1",
    });

    await expect(
      page.locator("#register-confirm-password").locator(".."),
    ).toContainText(/match/i);
  });
});
