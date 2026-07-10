import { test, expect } from "../../fixtures/auth.fixture";
import { loginAs } from "../../helpers/auth";
import { alice, SEED_PASSWORD } from "../../fixtures/seed-data";

test.describe("Route guards", () => {
  test.describe("unauthenticated", () => {
    const protectedRoutes = ["/", "/organizer/events", "/check-in"];

    for (const route of protectedRoutes) {
      test(`${route} redirects to login`, async ({ page }) => {
        await page.goto(route);
        await page.waitForURL(/\/login/);
      });
    }

    const publicRoutes = ["/checkout", "/tickets"];

    for (const route of publicRoutes) {
      test(`${route} is accessible without auth`, async ({ page }) => {
        const response = await page.goto(route);
        expect(response?.status()).toBeLessThan(400);
      });
    }
  });

  test.describe("authenticated", () => {
    test("visiting /login redirects to home", async ({
      authenticatedPage: page,
    }) => {
      await page.goto("/login");

      await page.waitForURL("/");
    });

    test("visiting /register redirects to home", async ({
      authenticatedPage: page,
    }) => {
      await page.goto("/register");

      await page.waitForURL("/");
    });

    test("protected routes are accessible", async ({
      authenticatedPage: page,
    }) => {
      await page.goto("/organizer/events");

      await expect(page).toHaveURL("/organizer/events");
    });
  });
});
