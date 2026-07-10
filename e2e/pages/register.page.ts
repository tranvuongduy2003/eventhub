import type { Locator, Page } from "@playwright/test";

export class RegisterPage {
  readonly page: Page;
  readonly displayNameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly submitButton: Locator;
  readonly errorAlert: Locator;

  constructor(page: Page) {
    this.page = page;
    this.displayNameInput = page.locator("#register-display-name");
    this.emailInput = page.locator("#register-email");
    this.passwordInput = page.locator("#register-password");
    this.confirmPasswordInput = page.locator("#register-confirm-password");
    this.submitButton = page.getByRole("button", { name: "Create account" });
    this.errorAlert = page.getByRole("alert");
  }

  async goto(): Promise<void> {
    await this.page.goto("/register");
  }

  async register(params: {
    displayName: string;
    email: string;
    password: string;
    confirmPassword?: string;
  }): Promise<void> {
    await this.displayNameInput.fill(params.displayName);
    await this.emailInput.fill(params.email);
    await this.passwordInput.fill(params.password);
    await this.confirmPasswordInput.fill(
      params.confirmPassword ?? params.password,
    );
    await this.submitButton.click();
  }

  async expectRootError(message: string): Promise<void> {
    await this.errorAlert.getByText(message).waitFor();
  }
}
