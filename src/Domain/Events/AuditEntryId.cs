using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public readonly record struct AuditEntryId(int Value)
{
    public static AuditEntryId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "AUDIT_ENTRY_ID_INVALID",
                "Audit entry id must be a positive integer.");
        }
        return new AuditEntryId(value);
    }

    public override string ToString() => Value.ToString();
}
