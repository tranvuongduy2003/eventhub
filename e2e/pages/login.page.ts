import type { Locator, Page } from "@playwright/test";

export class LoginPage {
  readonly page: Page;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorAlert: Locator;

  constructor(page: Page) {
    this.page = page;
    this.emailInput = page.locator("#login-email");
    this.passwordInput = page.locator("#login-password");
    this.submitButton = page.getByRole("button", { name: "Log in" });
    this.errorAlert = page.locator('[data-slot="alert"][data-variant="destructive"]');
  }

  async goto(): Promise<void> {
    await this.page.goto("/login");
  }

  async login(email: string, password: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  async expectEmailError(message: string): Promise<void> {
    await this.page
      .locator("#login-email")
      .locator("..")
      .getByText(message)
      .waitFor();
  }

  async expectPasswordError(message: string): Promise<void> {
    await this.page
      .locator("#login-password")
      .locator("..")
      .getByText(message)
      .waitFor();
  }

  async expectRootError(message: string): Promise<void> {
    await this.errorAlert.getByText(message).waitFor();
  }
}
