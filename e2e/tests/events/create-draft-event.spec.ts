import { test, expect } from "../../fixtures/auth.fixture";
import { CreateEventPage } from "../../pages/create-event.page";

test.describe("Create draft event", () => {
  let createEventPage: CreateEventPage;

  test.beforeEach(async ({ authenticatedPage: page }) => {
    createEventPage = new CreateEventPage(page);
    await createEventPage.goto();
  });

  test("organizer creates a draft event and sees events page", async ({
    authenticatedPage: page,
  }) => {
    const unique = Date.now();
    await createEventPage.createEvent({
      title: `Test Event ${unique}`,
      startDate: "2026-08-01",
      startTime: "14:00",
      endDate: "2026-08-01",
      endTime: "16:00",
      address: "123 Conference Ave, City",
    });

    await page.waitForURL("/events");
    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      "Events",
    );
  });

  test("organizer creates an online event", async ({
    authenticatedPage: page,
  }) => {
    const unique = Date.now();
    await createEventPage.createEvent({
      title: `Online Event ${unique}`,
      startDate: "2026-08-01",
      startTime: "14:00",
      endDate: "2026-08-01",
      endTime: "16:00",
      isOnline: true,
    });

    await page.waitForURL("/events");
  });

  test("empty title shows validation error", async ({
    authenticatedPage: page,
  }) => {
    await createEventPage.fillForm({
      title: "",
      startDate: "2026-08-01",
      startTime: "14:00",
      endDate: "2026-08-01",
      endTime: "16:00",
      address: "123 Main St",
    });
    await createEventPage.submit();

    await createEventPage.expectFieldError("Title is required.");
  });

  test("end before start shows validation error", async ({
    authenticatedPage: page,
  }) => {
    await createEventPage.fillForm({
      title: "Bad Timing Event",
      startDate: "2026-08-01",
      startTime: "16:00",
      endDate: "2026-08-01",
      endTime: "14:00",
      address: "123 Main St",
    });
    await createEventPage.submit();

    await createEventPage.expectFieldError("End must be after start.");
  });

  test("missing address for in-person event shows validation error", async ({
    authenticatedPage: page,
  }) => {
    await createEventPage.fillForm({
      title: "No Address Event",
      startDate: "2026-08-01",
      startTime: "14:00",
      endDate: "2026-08-01",
      endTime: "16:00",
    });
    await createEventPage.submit();

    await createEventPage.expectFieldError(
      "Address is required for in-person events.",
    );
  });

  test("unauthenticated user is redirected to login", async ({ page }) => {
    await page.goto("/events/create");
    await page.waitForURL(/\/login/);
  });
});
