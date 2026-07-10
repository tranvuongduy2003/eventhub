import { test, expect } from "../../fixtures/auth.fixture";
import { CreateEventPage } from "../../pages/create-event.page";

test.describe("Published event checkout", () => {
  test("organizer publishes a free event and attendee completes guest checkout", async ({
    authenticatedPage: page,
  }) => {
    const unique = Date.now();
    const title = `Free Checkout ${unique}`;
    const buyerEmail = `buyer_${unique}@example.com`;
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
    await expect(page.getByRole("heading", { name: "Events" })).toBeVisible();
    await expect(page.getByText(title)).toBeVisible();

    await page.getByRole("link", { name: `Edit ${title}` }).click();

    await expect(page.getByRole("heading", { name: "Edit event" })).toBeVisible();
    await page.getByRole("button", { name: "Add ticket type" }).click();
    await page.locator("#ticket-type-name").fill("General Admission");
    await page.locator("#ticket-type-price").fill("0");
    await page.locator("#ticket-type-currency").fill("VND");
    await page.locator("#ticket-type-capacity").fill("2");
    await page.locator("#ticket-type-max-per-order").fill("1");
    await page.getByRole("button", { name: "Add ticket type" }).click();

    await expect(page.getByText("General Admission")).toBeVisible();
    await expect(page.getByText(/Max 1 per order/)).toBeVisible();

    await page.getByRole("button", { name: "Publish event" }).click();
    await expect(page.getByText("This event is published.")).toBeVisible();

    await page.goto(`/events?q=${encodeURIComponent(title)}`);
    await expect(page.getByRole("heading", { name: "Events" })).toBeVisible();
    await expect(page.getByRole("link", { name: new RegExp(title) })).toBeVisible();
    await page.getByRole("link", { name: new RegExp(title) }).click();

    await expect(page.getByRole("heading", { name: title })).toBeVisible();
    await expect(page.getByText("All-inclusive, no hidden fees")).toBeVisible();
    await page.getByRole("button", { name: "Increase General Admission" }).click();
    await page.getByRole("button", { name: "Continue to checkout" }).click();

    await expect(page.getByRole("heading", { name: title })).toBeVisible();
    await expect(page.getByText("Guest checkout")).toBeVisible();
    await expect(page.getByText("General Admission")).toBeVisible();
    await page.locator("#guest-contact-name").fill("Pat Buyer");
    await page.locator("#guest-contact-email").fill(buyerEmail);
    await page.getByRole("button", { name: "Continue as guest" }).click();

    await expect(page.getByText("Order accepted")).toBeVisible();
    await expect(page.getByText("Status: Confirmed")).toBeVisible();
    await page.getByRole("link", { name: "View order status" }).click();

    await expect(page.getByRole("heading", { name: /Order #/ })).toBeVisible();
    await expect(page.locator("span").filter({ hasText: "Confirmed" })).toBeVisible();
    await expect(page.getByText("General Admission")).toBeVisible();
    await page.getByRole("link", { name: "View tickets" }).click();

    await expect(page.getByRole("heading", { name: /Order #/ })).toBeVisible();
    await expect(page.getByRole("img", { name: /QR code for ticket/ })).toBeVisible();
    await expect(page.getByText("General Admission")).toBeVisible();
    await expect(page.getByText(buyerEmail)).toBeVisible();
  });
});
