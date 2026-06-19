using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class EventLocation : ValueObject
{
    private const int MaxAddressLength = 500;

    private EventLocation(string? physicalAddress, bool isOnline)
    {
        PhysicalAddress = physicalAddress;
        IsOnline = isOnline;
    }

    public string? PhysicalAddress { get; }

    public bool IsOnline { get; }

    public static EventLocation Create(string? physicalAddress, bool isOnline)
    {
        var hasAddress = !string.IsNullOrWhiteSpace(physicalAddress);

        if (!hasAddress && !isOnline)
        {
            throw new BusinessRuleValidationException(
                "EVENT_LOCATION_REQUIRED",
                "Event must have a physical address or be marked as online.");
        }

        if (hasAddress && isOnline)
        {
            throw new BusinessRuleValidationException(
                "EVENT_LOCATION_REQUIRED",
                "Event cannot have both a physical address and be marked as online.");
        }

        if (hasAddress && physicalAddress!.Trim().Length > MaxAddressLength)
        {
            throw new BusinessRuleValidationException(
                "EVENT_LOCATION_REQUIRED",
                $"Physical address cannot exceed {MaxAddressLength} characters.");
        }

        return new EventLocation(
            hasAddress ? physicalAddress!.Trim() : null,
            isOnline);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PhysicalAddress;
        yield return IsOnline;
    }
}
