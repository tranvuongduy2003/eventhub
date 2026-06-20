import { test as base, type Page } from "@playwright/test";
import { alice, SEED_PASSWORD, type SeedUser } from "./seed-data";
import { loginAs } from "../helpers/auth";

interface AuthFixtures {
  authenticatedPage: Page;
  seedUser: SeedUser;
}

export const test = base.extend<AuthFixtures>({
  seedUser: async ({}, use) => {
    await use(alice);
  },

  authenticatedPage: async ({ page, seedUser }, use) => {
    await loginAs(page, seedUser);
    await use(page);
  },
});

export { expect } from "@playwright/test";
