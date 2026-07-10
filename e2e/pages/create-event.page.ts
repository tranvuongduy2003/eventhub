import type { Locator, Page } from "@playwright/test";

export class CreateEventPage {
  readonly page: Page;
  readonly titleInput: Locator;
  readonly startDateInput: Locator;
  readonly startTimeInput: Locator;
  readonly endDateInput: Locator;
  readonly endTimeInput: Locator;
  readonly timezoneSelect: Locator;
  readonly isOnlineSwitch: Locator;
  readonly addressInput: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly errorAlert: Locator;

  constructor(page: Page) {
    this.page = page;
    this.titleInput = page.locator("#create-event-title");
    this.startDateInput = page.locator("#create-event-start-date");
    this.startTimeInput = page.locator("#create-event-start-time");
    this.endDateInput = page.locator("#create-event-end-date");
    this.endTimeInput = page.locator("#create-event-end-time");
    this.timezoneSelect = page.locator("#create-event-timezone");
    this.isOnlineSwitch = page.getByText("Online event");
    this.addressInput = page.locator("#create-event-address");
    this.submitButton = page.getByRole("button", { name: "Create event" });
    this.cancelButton = page.getByRole("button", { name: "Cancel" });
    this.errorAlert = page.locator(
      '[data-slot="alert"][data-variant="destructive"]',
    );
  }

  async goto(): Promise<void> {
    await this.page.goto("/organizer/events/create");
  }

  async fillForm(params: {
    title: string;
    startDate: string;
    startTime: string;
    endDate: string;
    endTime: string;
    address?: string;
    isOnline?: boolean;
  }): Promise<void> {
    await this.titleInput.fill(params.title);
    await this.startDateInput.fill(params.startDate);
    await this.startTimeInput.fill(params.startTime);
    await this.endDateInput.fill(params.endDate);
    await this.endTimeInput.fill(params.endTime);

    if (params.isOnline) {
      await this.isOnlineSwitch.click();
    } else if (params.address) {
      await this.addressInput.fill(params.address);
    }
  }

  async submit(): Promise<void> {
    await this.submitButton.click();
  }

  async createEvent(params: {
    title: string;
    startDate: string;
    startTime: string;
    endDate: string;
    endTime: string;
    address?: string;
    isOnline?: boolean;
  }): Promise<void> {
    await this.fillForm(params);
    await this.submit();
  }

  async expectFieldError(message: string): Promise<void> {
    await this.page.getByText(message).first().waitFor();
  }

  async expectRootError(message: string): Promise<void> {
    await this.errorAlert.getByText(message).waitFor();
  }
}
