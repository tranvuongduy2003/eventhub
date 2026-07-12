import { test, expect } from "../../fixtures/auth.fixture";
import { CreateEventPage } from "../../pages/create-event.page";

test.describe("Live sales inventory", () => {
  test("organizer results update after guest checkout without refreshing", async ({
    authenticatedPage: organizerPage,
    browser,
  }) => {
    const unique = Date.now();
    const title = `Live Sales ${unique}`;
    const buyerEmail = `live_buyer_${unique}@example.com`;
    const createEventPage = new CreateEventPage(organizerPage);

    await createEventPage.goto();
    await createEventPage.createEvent({
      title,
      startDate: "2026-08-15",
      startTime: "14:00",
      endDate: "2026-08-15",
      endTime: "16:00",
      isOnline: true,
    });

    await organizerPage.waitForURL("/organizer/events");
    await organizerPage.getByRole("link", { name: `Edit ${title}` }).click();
    await expect(organizerPage.getByRole("heading", { name: "Edit event" })).toBeVisible();

    const editUrl = organizerPage.url();
    const eventId = editUrl.match(/\/organizer\/events\/(\d+)\/edit/)?.[1];
    expect(eventId).toBeTruthy();

    await organizerPage.getByRole("button", { name: "Add ticket type" }).click();
    await organizerPage.locator("#ticket-type-name").fill("General Admission");
    await organizerPage.locator("#ticket-type-price").fill("0");
    await organizerPage.locator("#ticket-type-currency").fill("VND");
    await organizerPage.locator("#ticket-type-capacity").fill("2");
    await organizerPage.locator("#ticket-type-max-per-order").fill("1");
    await organizerPage.getByRole("button", { name: "Add ticket type" }).click();
    await expect(organizerPage.getByText("General Admission")).toBeVisible();

    await organizerPage.getByRole("button", { name: "Publish event" }).click();
    await expect(organizerPage.getByText("This event is published.")).toBeVisible();

    const resultsWebSocket = organizerPage.waitForEvent("websocket");
    await organizerPage.goto(`/organizer/events/${eventId}/results`);
    await expect(organizerPage.getByRole("heading", { name: title })).toBeVisible();
    await resultsWebSocket;

    const ticketTypeRow = organizerPage
      .getByRole("row")
      .filter({ has: organizerPage.getByText("General Admission") });
    await expect(ticketTypeRow.getByRole("cell").nth(1)).toHaveText("0");
    await expect(ticketTypeRow.getByRole("cell").nth(2)).toHaveText("2 / 2");

    const guestContext = await browser.newContext({ ignoreHTTPSErrors: true });
    const guestPage = await guestContext.newPage();
    try {
      await guestPage.goto(`/events?q=${encodeURIComponent(title)}`);
      await expect(guestPage.getByRole("heading", { name: "Events" })).toBeVisible();
      await guestPage.getByRole("link", { name: new RegExp(title) }).click();

      await expect(guestPage.getByRole("heading", { name: title })).toBeVisible();
      await guestPage.getByRole("button", { name: "Increase General Admission" }).click();
      await guestPage.getByRole("button", { name: "Continue to checkout" }).click();

      await expect(guestPage.getByText("Guest checkout")).toBeVisible();
      await guestPage.locator("#guest-contact-name").fill("Live Buyer");
      await guestPage.locator("#guest-contact-email").fill(buyerEmail);
      await guestPage.getByRole("button", { name: "Continue as guest" }).click();

      await expect(guestPage.getByText("Order accepted")).toBeVisible();
      await expect(guestPage.getByText("Status: Confirmed")).toBeVisible();
    } finally {
      await guestContext.close();
    }

    await expect(ticketTypeRow.getByRole("cell").nth(1)).toHaveText("1");
    await expect(ticketTypeRow.getByRole("cell").nth(2)).toHaveText("1 / 2");
    await expect(organizerPage).toHaveURL(`/organizer/events/${eventId}/results`);
  });
});
