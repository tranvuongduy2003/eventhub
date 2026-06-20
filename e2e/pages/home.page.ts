import type { Locator, Page } from "@playwright/test";

export class HomePage {
  readonly page: Page;
  readonly welcomeHeading: Locator;
  readonly accountStatus: Locator;
  readonly emailCard: Locator;

  constructor(page: Page) {
    this.page = page;
    this.welcomeHeading = page.getByRole("heading", { level: 1 });
    this.accountStatus = page.getByText("Verified organizer");
    this.emailCard = page.getByText("Email");
  }

  async goto(): Promise<void> {
    await this.page.goto("/");
  }

  async expectWelcomeMessage(displayName: string): Promise<void> {
    await this.welcomeHeading
      .getByText(`Welcome back, ${displayName}`)
      .waitFor();
  }

  async expectDashboardVisible(): Promise<void> {
    await this.accountStatus.waitFor();
  }
}
