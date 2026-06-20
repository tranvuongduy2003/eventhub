import type { Page } from "@playwright/test";
import type { SeedUser } from "../fixtures/seed-data";
import { SEED_PASSWORD } from "../fixtures/seed-data";

export async function loginAs(page: Page, user: SeedUser): Promise<void> {
  await page.goto("/login");
  await page.locator("#login-email").fill(user.email);
  await page.locator("#login-password").fill(SEED_PASSWORD);
  await page.getByRole("button", { name: "Log in" }).click();
  await page.waitForURL("/");
}

export async function registerUser(
  page: Page,
  params: { displayName: string; email: string; password: string },
): Promise<void> {
  await page.goto("/register");
  await page.locator("#register-display-name").fill(params.displayName);
  await page.locator("#register-email").fill(params.email);
  await page.locator("#register-password").fill(params.password);
  await page.locator("#register-confirm-password").fill(params.password);
  await page.getByRole("button", { name: "Create account" }).click();
  await page.waitForURL("/");
}

export async function logoutViaApi(page: Page): Promise<void> {
  await page.request.post("/api/auth/logout");
}
