using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class SalesWindow : ValueObject
{
    private SalesWindow(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public static SalesWindow Create(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new BusinessRuleValidationException(
                "SALES_WINDOW_INVALID",
                "Sales window end must be after start.");
        }

        return new SalesWindow(start, end);
    }

    public bool IsOpen(DateTimeOffset now) => now >= Start && now <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
