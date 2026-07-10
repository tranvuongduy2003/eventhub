using System.Text.RegularExpressions;
using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Tickets;

public sealed partial class TicketCode : ValueObject
{
    private TicketCode()
    {
    }

    public string Value { get; private set; } = null!;

    public static TicketCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "TICKET_CODE_REQUIRED",
                "Ticket code is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length is < 24 or > 120 || !TicketCodePattern().IsMatch(trimmed))
        {
            throw new BusinessRuleValidationException(
                "TICKET_CODE_INVALID",
                "Ticket code format is invalid.");
        }

        return new TicketCode { Value = trimmed };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.Compiled)]
    private static partial Regex TicketCodePattern();
}
