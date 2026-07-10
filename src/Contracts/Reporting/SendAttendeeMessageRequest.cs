namespace EventHub.Contracts.Reporting;

public sealed record SendAttendeeMessageRequest(string Subject, string Body);

public sealed record SendAttendeeMessageResponse(int AcceptedRecipientCount);
