using EventHub.Application.Abstractions.Email;

namespace EventHub.Application.Tickets;

internal static class TicketEmailComposer
{
    public static EmailMessage Create(string recipient, IReadOnlyCollection<TicketResult> tickets)
    {
        var first = tickets.First();
        var ticketItems = string.Join(
            "",
            tickets.Select(ticket =>
                $"""
                <li>
                    <strong>{Escape(ticket.TicketTypeName)}</strong><br />
                    Code: <code>{Escape(ticket.Code)}</code><br />
                    Ticket link: /tickets/orders/{ticket.OrderId}
                </li>
                """));

        var location = first.EventIsOnline ? "Online" : first.EventLocation ?? "Location to be announced";
        var body =
            $"""
            <h1>Your tickets for {Escape(first.EventTitle)}</h1>
            <p>{first.EventStartsAt:u} ({Escape(first.EventTimeZoneId)})</p>
            <p>{Escape(location)}</p>
            <ul>{ticketItems}</ul>
            """;

        return new EmailMessage(
            recipient,
            $"Your tickets for {first.EventTitle}",
            body);
    }

    private static string Escape(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
