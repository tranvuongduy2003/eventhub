import type { Page } from "@playwright/test";
import { test, expect } from "../../fixtures/auth.fixture";
import { CreateEventPage } from "../../pages/create-event.page";

test.describe("Check-in and ticket returns", () => {
  test("organizer scans tickets, checks in manually, and syncs queued scans", async ({
    authenticatedPage: page,
  }) => {
    const unique = Date.now();
    const title = `Door Ops ${unique}`;
    const buyerEmail = `door_buyer_${unique}@example.com`;
    const { eventId } = await createPublishedEvent(page, title, {
      capacity: 3,
      maxPerOrder: 3,
    });

    const ticketCodes = await checkoutTickets(page, title, buyerEmail, 3);
    expect(ticketCodes).toHaveLength(3);

    await page.goto(`/check-in?eventId=${eventId}`);
    await expect(page.getByRole("heading", { name: "Check-in" })).toBeVisible();
    await expect(countCard(page, "Checked in")).toHaveText("0");
    await expect(countCard(page, "Issued")).toHaveText("3");

    await page.locator("#ticket-code").fill(ticketCodes[0]);
    await page.getByRole("button", { name: "Check in" }).click();
    await expect(countCard(page, "Checked in")).toHaveText("1");

    await page.locator("#check-in-search").fill(buyerEmail);
    const manualRow = page
      .getByRole("row")
      .filter({ has: page.getByText(ticketCodes[1]) });
    await expect(manualRow).toBeVisible();
    await manualRow.getByRole("button", { name: "Check in" }).click();
    await expect(countCard(page, "Checked in")).toHaveText("2");

    await page.locator("#ticket-code").fill(ticketCodes[2]);
    await page.getByRole("button", { name: "Queue offline" }).click();
    await expect(page.getByText("1 waiting to sync")).toBeVisible();
    await page.getByRole("button", { name: "Sync" }).click();
    await expect(page.getByText("0 waiting to sync")).toBeVisible();
    await expect(countCard(page, "Checked in")).toHaveText("3");
  });

  test("attendee returns a ticket to the sales pool", async ({ authenticatedPage: page }) => {
    const unique = Date.now();
    const title = `Return Pool ${unique}`;
    const buyerEmail = `return_buyer_${unique}@example.com`;
    await createPublishedEvent(page, title, {
      capacity: 1,
      maxPerOrder: 1,
    });

    await checkoutTickets(page, title, buyerEmail, 1);
    await expect(page.getByRole("button", { name: "Return ticket" })).toBeEnabled();
    await page.getByRole("button", { name: "Return ticket" }).click();
    await expect(page.getByText("void")).toBeVisible();
    await expect(page.getByRole("button", { name: "Return ticket" })).toBeDisabled();

    await page.goto(`/events?q=${encodeURIComponent(title)}`);
    await page.getByRole("link", { name: new RegExp(title) }).click();
    await expect(page.getByRole("button", { name: "Increase General Admission" })).toBeEnabled();
  });
});

async function createPublishedEvent(
  page: Page,
  title: string,
  options: { capacity: number; maxPerOrder: number },
): Promise<{ eventId: string }> {
  const createEventPage = new CreateEventPage(page);
  await createEventPage.goto();
  await createEventPage.createEvent({
    title,
    startDate: "2026-08-15",
    startTime: "14:00",
    endDate: "2026-08-15",
    endTime: "16:00",
    isOnline: true,
  });

  await page.waitForURL("/organizer/events");
  await page.getByRole("link", { name: `Edit ${title}` }).click();
  await expect(page.getByRole("heading", { name: "Edit event" })).toBeVisible();

  const eventId = page.url().match(/\/organizer\/events\/(\d+)\/edit/)?.[1];
  expect(eventId).toBeTruthy();

  await page.getByRole("button", { name: "Add ticket type" }).click();
  await page.locator("#ticket-type-name").fill("General Admission");
  await page.locator("#ticket-type-price").fill("0");
  await page.locator("#ticket-type-currency").fill("VND");
  await page.locator("#ticket-type-capacity").fill(String(options.capacity));
  await page.locator("#ticket-type-max-per-order").fill(String(options.maxPerOrder));
  await page.getByRole("button", { name: "Add ticket type" }).click();
  await expect(page.getByText("General Admission")).toBeVisible();

  await page.getByRole("button", { name: "Publish event" }).click();
  await expect(page.getByText("This event is published.")).toBeVisible();

  return { eventId: eventId! };
}

async function checkoutTickets(
  page: Page,
  title: string,
  buyerEmail: string,
  quantity: number,
): Promise<string[]> {
  await page.goto(`/events?q=${encodeURIComponent(title)}`);
  await expect(page.getByRole("heading", { name: "Events" })).toBeVisible();
  await page.getByRole("link", { name: new RegExp(title) }).click();

  await expect(page.getByRole("heading", { name: title })).toBeVisible();
  for (let index = 0; index < quantity; index += 1) {
    await page.getByRole("button", { name: "Increase General Admission" }).click();
  }
  await page.getByRole("button", { name: "Continue to checkout" }).click();

  await expect(page.getByText("Guest checkout")).toBeVisible();
  await page.locator("#guest-contact-name").fill("E2E Buyer");
  await page.locator("#guest-contact-email").fill(buyerEmail);
  await page.getByRole("button", { name: "Continue as guest" }).click();

  await expect(page.getByText("Order accepted")).toBeVisible();
  await expect(page.getByText("Status: Confirmed")).toBeVisible();
  await page.getByRole("link", { name: "View order status" }).click();
  await page.getByRole("link", { name: "View tickets" }).click();

  const ticketCodes = page.locator("code");
  await expect(ticketCodes).toHaveCount(quantity);
  return (await ticketCodes.allTextContents()).map((code) => code.trim());
}

function countCard(page: Page, title: string) {
  return page
    .locator('[data-slot="card"]')
    .filter({ has: page.locator('[data-slot="card-title"]', { hasText: title }) })
    .locator('[data-slot="card-content"]');
}
