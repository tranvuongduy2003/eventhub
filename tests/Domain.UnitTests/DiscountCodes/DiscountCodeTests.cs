using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.DiscountCodes;

public sealed class DiscountCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    // --- DiscountCode.Create ---

    [Fact]
    public void Create_ValidPercentageCode_SetsProperties()
    {
        var dc = DiscountCode.Create(1, "EARLY20", DiscountCodeType.Percentage, 20m, null, null, null, Now);

        dc.EventId.Should().Be(1);
        dc.Code.Should().Be("EARLY20");
        dc.Type.Should().Be(DiscountCodeType.Percentage);
        dc.Value.Should().Be(20m);
        dc.StartAt.Should().BeNull();
        dc.EndAt.Should().BeNull();
        dc.UsageCap.Should().BeNull();
        dc.UsedCount.Should().Be(0);
        dc.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_ValidFixedAmountCode_SetsProperties()
    {
        var dc = DiscountCode.Create(1, "SAVE50K", DiscountCodeType.FixedAmount, 50000m, null, null, null, Now);

        dc.Type.Should().Be(DiscountCodeType.FixedAmount);
        dc.Value.Should().Be(50000m);
    }

    [Fact]
    public void Create_WithDateWindow_SetsDates()
    {
        var start = Now.AddDays(-1);
        var end = Now.AddDays(7);

        var dc = DiscountCode.Create(1, "SUMMER", DiscountCodeType.Percentage, 10m, start, end, null, Now);

        dc.StartAt.Should().Be(start);
        dc.EndAt.Should().Be(end);
    }

    [Fact]
    public void Create_WithUsageCap_SetsCap()
    {
        var dc = DiscountCode.Create(1, "LIMITED", DiscountCodeType.Percentage, 15m, null, null, 100, Now);

        dc.UsageCap.Should().Be(100);
    }

    [Fact]
    public void Create_LowercaseCode_NormalizesToUppercase()
    {
        var dc = DiscountCode.Create(1, "early20", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        dc.Code.Should().Be("EARLY20");
    }

    [Fact]
    public void Create_CodeWithSpaces_TrimsAndNormalizes()
    {
        var dc = DiscountCode.Create(1, "  SUMMER  ", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        dc.Code.Should().Be("SUMMER");
    }

    [Fact]
    public void Create_EmptyCode_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_EMPTY");
    }

    [Fact]
    public void Create_CodeTooShort_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "AB", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_LENGTH");
    }

    [Fact]
    public void Create_CodeTooLong_ThrowsBusinessRuleValidationException()
    {
        var longCode = new string('A', 31);

        var act = () => DiscountCode.Create(1, longCode, DiscountCodeType.Percentage, 10m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_LENGTH");
    }

    [Fact]
    public void Create_CodeWithSpecialChars_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "EARLY-20", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_FORMAT");
    }

    [Fact]
    public void Create_PercentageBelow1_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 0m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_PERCENTAGE");
    }

    [Fact]
    public void Create_PercentageAbove100_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 101m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_PERCENTAGE");
    }

    [Fact]
    public void Create_FixedAmountZero_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "CODE", DiscountCodeType.FixedAmount, 0m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_AMOUNT");
    }

    [Fact]
    public void Create_FixedAmountNegative_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "CODE", DiscountCodeType.FixedAmount, -10m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_AMOUNT");
    }

    [Fact]
    public void Create_EndBeforeStart_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, Now, Now.AddDays(-1), null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_DATE_WINDOW");
    }

    [Fact]
    public void Create_ZeroUsageCap_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, 0, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_INVALID_USAGE_CAP");
    }

    // --- DiscountCode.Edit ---

    [Fact]
    public void Edit_ValidChanges_UpdatesProperties()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        dc.Edit(DiscountCodeType.FixedAmount, 50000m, Now, Now.AddDays(30), 50, Now.AddDays(1));

        dc.Type.Should().Be(DiscountCodeType.FixedAmount);
        dc.Value.Should().Be(50000m);
        dc.StartAt.Should().Be(Now);
        dc.EndAt.Should().Be(Now.AddDays(30));
        dc.UsageCap.Should().Be(50);
        dc.UpdatedAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void Edit_DeletedCode_ThrowsBusinessRuleValidationException()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);
        dc.Delete(Now);

        var act = () => dc.Edit(DiscountCodeType.Percentage, 20m, null, null, null, Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_DELETED");
    }

    // --- DiscountCode.MarkUsed ---

    [Fact]
    public void MarkUsed_ActiveCode_IncrementsUsedCount()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        dc.MarkUsed(Now);

        dc.UsedCount.Should().Be(1);
    }

    [Fact]
    public void MarkUsed_WithinCap_IncrementsUsedCount()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, 10, Now);

        dc.MarkUsed(Now);

        dc.UsedCount.Should().Be(1);
    }

    [Fact]
    public void MarkUsed_AtCap_ThrowsBusinessRuleValidationException()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, 1, Now);
        dc.MarkUsed(Now);

        var act = () => dc.MarkUsed(Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_EXHAUSTED");
    }

    [Fact]
    public void MarkUsed_ExpiredCode_ThrowsBusinessRuleValidationException()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, Now.AddDays(-1), null, Now);

        var act = () => dc.MarkUsed(Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_NOT_ACTIVE");
    }

    [Fact]
    public void MarkUsed_NotYetValid_ThrowsBusinessRuleValidationException()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, Now.AddDays(1), null, null, Now);

        var act = () => dc.MarkUsed(Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_NOT_ACTIVE");
    }

    [Fact]
    public void MarkUsed_DeletedCode_ThrowsBusinessRuleValidationException()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);
        dc.Delete(Now);

        var act = () => dc.MarkUsed(Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_DELETED");
    }

    // --- DiscountCode.Delete ---

    [Fact]
    public void Delete_ActiveCode_SetsDeletedAt()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        dc.Delete(Now);

        dc.DeletedAt.Should().Be(Now);
    }

    [Fact]
    public void Delete_AlreadyDeleted_ThrowsBusinessRuleValidationException()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);
        dc.Delete(Now);

        var act = () => dc.Delete(Now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_ALREADY_DELETED");
    }

    // --- DiscountCode.IsActive ---

    [Fact]
    public void IsActive_NoRestrictions_ReturnsTrue()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);

        dc.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActive_WithinDateWindow_ReturnsTrue()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, Now.AddDays(-1), Now.AddDays(1), null, Now);

        dc.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActive_BeforeStart_ReturnsFalse()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, Now.AddDays(1), null, null, Now);

        dc.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void IsActive_AfterEnd_ReturnsFalse()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, Now.AddDays(-1), null, Now);

        dc.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void IsActive_Exhausted_ReturnsFalse()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, 1, Now);
        dc.MarkUsed(Now);

        dc.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void IsActive_Deleted_ReturnsFalse()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 10m, null, null, null, Now);
        dc.Delete(Now);

        dc.IsActive(Now).Should().BeFalse();
    }

    // --- DiscountCode.ComputeDiscount ---

    [Fact]
    public void ComputeDiscount_Percentage_CalculatesCorrectly()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 20m, null, null, null, Now);
        var total = Money.Create(100000m, "VND");

        var discount = dc.ComputeDiscount(total);

        discount.Amount.Should().Be(20000m);
        discount.Currency.Should().Be("VND");
    }

    [Fact]
    public void ComputeDiscount_FixedAmount_SubtractsFromTotal()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.FixedAmount, 30000m, null, null, null, Now);
        var total = Money.Create(100000m, "VND");

        var discount = dc.ComputeDiscount(total);

        discount.Amount.Should().Be(30000m);
    }

    [Fact]
    public void ComputeDiscount_FixedAmountExceedsTotal_ClampsAtTotal()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.FixedAmount, 200000m, null, null, null, Now);
        var total = Money.Create(100000m, "VND");

        var discount = dc.ComputeDiscount(total);

        discount.Amount.Should().Be(100000m);
    }

    [Fact]
    public void ComputeDiscount_100Percent_ReducesToZero()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 100m, null, null, null, Now);
        var total = Money.Create(100000m, "VND");

        var discount = dc.ComputeDiscount(total);

        discount.Amount.Should().Be(100000m);
    }

    [Fact]
    public void ComputeDiscount_ZeroTotal_ReturnsZero()
    {
        var dc = DiscountCode.Create(1, "CODE", DiscountCodeType.Percentage, 50m, null, null, null, Now);
        var total = Money.Create(0, "VND");

        var discount = dc.ComputeDiscount(total);

        discount.Amount.Should().Be(0);
    }

    // --- DiscountCodeId ---

    [Fact]
    public void DiscountCodeId_ValidInput_CreatesId()
    {
        var id = DiscountCodeId.From(1);

        id.Value.Should().Be(1);
    }

    [Fact]
    public void DiscountCodeId_ZeroValue_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCodeId.From(0);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_ID_INVALID");
    }

    [Fact]
    public void DiscountCodeId_NegativeValue_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCodeId.From(-1);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_ID_INVALID");
    }

    // --- DiscountCode.NormalizeCode ---

    [Fact]
    public void NormalizeCode_Lowercase_ReturnsUppercase()
    {
        DiscountCode.NormalizeCode("abc").Should().Be("ABC");
    }

    [Fact]
    public void NormalizeCode_MixedCase_ReturnsUppercase()
    {
        DiscountCode.NormalizeCode("AbCdE").Should().Be("ABCDE");
    }

    [Fact]
    public void NormalizeCode_WithSpaces_TrimsAndUppercases()
    {
        DiscountCode.NormalizeCode("  hello  ").Should().Be("HELLO");
    }

    [Fact]
    public void NormalizeCode_Empty_ThrowsBusinessRuleValidationException()
    {
        var act = () => DiscountCode.NormalizeCode("");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISCOUNT_CODE_EMPTY");
    }
}
