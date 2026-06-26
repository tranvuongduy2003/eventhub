using System.Text.RegularExpressions;
using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.DiscountCodes;

public sealed partial class DiscountCode : AggregateRoot<DiscountCodeId>
{
    private static readonly Regex CodePattern = AlphaNumericOnly();

    private DiscountCode()
    {
    }

    public int EventId { get; private set; }

    public string Code { get; private set; } = null!;

    public DiscountCodeType Type { get; private set; }

    public decimal Value { get; private set; }

    public DateTimeOffset? StartAt { get; private set; }

    public DateTimeOffset? EndAt { get; private set; }

    public int? UsageCap { get; private set; }

    public int UsedCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public long RowVersion { get; private set; }

    public static DiscountCode Create(
        int eventId,
        string code,
        DiscountCodeType type,
        decimal value,
        DateTimeOffset? startAt,
        DateTimeOffset? endAt,
        int? usageCap,
        DateTimeOffset createdAt)
    {
        var normalizedCode = NormalizeCode(code);

        ValidateCodeFormat(normalizedCode);
        ValidateTypeAndValue(type, value);
        ValidateDateWindow(startAt, endAt);
        ValidateUsageCap(usageCap);

        return new DiscountCode
        {
            EventId = eventId,
            Code = normalizedCode,
            Type = type,
            Value = value,
            StartAt = startAt,
            EndAt = endAt,
            UsageCap = usageCap,
            UsedCount = 0,
            CreatedAt = createdAt,
            UpdatedAt = null,
            DeletedAt = null,
            RowVersion = 1,
        };
    }

    public void Edit(
        DiscountCodeType type,
        decimal value,
        DateTimeOffset? startAt,
        DateTimeOffset? endAt,
        int? usageCap,
        DateTimeOffset updatedAt)
    {
        if (DeletedAt.HasValue)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_DELETED",
                "Cannot edit a deleted discount code.");
        }

        ValidateTypeAndValue(type, value);
        ValidateDateWindow(startAt, endAt);
        ValidateUsageCap(usageCap);

        Type = type;
        Value = value;
        StartAt = startAt;
        EndAt = endAt;
        UsageCap = usageCap;
        UpdatedAt = updatedAt;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        if (DeletedAt.HasValue)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_DELETED",
                "Cannot use a deleted discount code.");
        }

        if (UsageCap.HasValue && UsedCount >= UsageCap.Value)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_EXHAUSTED",
                "The discount code has reached its usage cap.");
        }

        if (StartAt.HasValue && usedAt < StartAt.Value)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_NOT_ACTIVE",
                "The discount code is not yet valid.");
        }

        if (EndAt.HasValue && usedAt > EndAt.Value)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_NOT_ACTIVE",
                "The discount code has expired.");
        }

        UsedCount++;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        if (DeletedAt.HasValue)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_ALREADY_DELETED",
                "The discount code is already deleted.");
        }

        DeletedAt = deletedAt;
    }

    public bool IsActive(DateTimeOffset at)
    {
        if (DeletedAt.HasValue)
        {
            return false;
        }

        if (StartAt.HasValue && at < StartAt.Value)
        {
            return false;
        }

        if (EndAt.HasValue && at > EndAt.Value)
        {
            return false;
        }

        if (UsageCap.HasValue && UsedCount >= UsageCap.Value)
        {
            return false;
        }

        return true;
    }

    public Money ComputeDiscount(Money orderTotal)
    {
        if (orderTotal.Amount <= 0)
        {
            return Money.Create(0, orderTotal.Currency);
        }

        decimal discountAmount = Type switch
        {
            DiscountCodeType.Percentage => Math.Round(orderTotal.Amount * (Value / 100m), 2),
            DiscountCodeType.FixedAmount => Value,
            _ => 0m,
        };

        // INV-20: clamp at zero — discount cannot exceed order total
        discountAmount = Math.Min(discountAmount, orderTotal.Amount);

        return Money.Create(discountAmount, orderTotal.Currency);
    }

    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_EMPTY",
                "Discount code cannot be empty.");
        }

        return code.Trim().ToUpperInvariant();
    }

    public static DiscountCode FromPersistence(
        DiscountCodeId id,
        int eventId,
        string code,
        DiscountCodeType type,
        decimal value,
        DateTimeOffset? startAt,
        DateTimeOffset? endAt,
        int? usageCap,
        int usedCount,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        DateTimeOffset? deletedAt,
        long rowVersion) =>
        new()
        {
            Id = id,
            EventId = eventId,
            Code = code,
            Type = type,
            Value = value,
            StartAt = startAt,
            EndAt = endAt,
            UsageCap = usageCap,
            UsedCount = usedCount,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            RowVersion = rowVersion,
        };

    private static void ValidateCodeFormat(string normalizedCode)
    {
        if (normalizedCode.Length < 3 || normalizedCode.Length > 30)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_INVALID_LENGTH",
                "Discount code must be between 3 and 30 characters.");
        }

        if (!CodePattern.IsMatch(normalizedCode))
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_INVALID_FORMAT",
                "Discount code must contain only letters and digits.");
        }
    }

    private static void ValidateTypeAndValue(DiscountCodeType type, decimal value)
    {
        if (type == DiscountCodeType.Percentage)
        {
            if (value < 1 || value > 100)
            {
                throw new BusinessRuleValidationException(
                    "DISCOUNT_CODE_INVALID_PERCENTAGE",
                    "Percentage discount value must be between 1 and 100.");
            }
        }
        else if (type == DiscountCodeType.FixedAmount)
        {
            if (value <= 0)
            {
                throw new BusinessRuleValidationException(
                    "DISCOUNT_CODE_INVALID_AMOUNT",
                    "Fixed-amount discount value must be greater than zero.");
            }
        }
    }

    private static void ValidateDateWindow(DateTimeOffset? startAt, DateTimeOffset? endAt)
    {
        if (startAt.HasValue && endAt.HasValue && endAt.Value <= startAt.Value)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_INVALID_DATE_WINDOW",
                "End date must be after start date.");
        }
    }

    private static void ValidateUsageCap(int? usageCap)
    {
        if (usageCap.HasValue && usageCap.Value <= 0)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_INVALID_USAGE_CAP",
                "Usage cap must be at least 1 when set.");
        }
    }

    [GeneratedRegex("^[A-Z0-9]+$")]
    private static partial Regex AlphaNumericOnly();
}
