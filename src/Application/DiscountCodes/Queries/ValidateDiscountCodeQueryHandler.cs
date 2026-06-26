using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;

namespace EventHub.Application.DiscountCodes.Queries;

public sealed class ValidateDiscountCodeQueryHandler(
    IDiscountCodeRepository discountCodeRepository,
    IClock clock)
    : QueryHandler<ValidateDiscountCodeQuery, ValidateDiscountCodeResult>
{
    public override async Task<Result<ValidateDiscountCodeResult>> Handle(
        ValidateDiscountCodeQuery query,
        CancellationToken cancellationToken)
    {
        var normalizedCode = DiscountCode.NormalizeCode(query.Code);

        var discountCode = await discountCodeRepository.GetByCodeAsync(
            query.EventId, normalizedCode, cancellationToken);

        if (discountCode is null || discountCode.DeletedAt.HasValue)
        {
            return DiscountCodeErrors.NotFound;
        }

        var now = clock.UtcNow;

        if (discountCode.StartAt.HasValue && now < discountCode.StartAt.Value)
        {
            return DiscountCodeErrors.NotYetValid;
        }

        if (discountCode.EndAt.HasValue && now > discountCode.EndAt.Value)
        {
            return DiscountCodeErrors.Expired;
        }

        if (discountCode.UsageCap.HasValue && discountCode.UsedCount >= discountCode.UsageCap.Value)
        {
            return DiscountCodeErrors.Exhausted;
        }

        var orderTotal = Money.Create(query.OrderTotalAmount, query.OrderTotalCurrency);
        var discountAmount = discountCode.ComputeDiscount(orderTotal);
        var finalTotal = Money.Create(
            Math.Max(0, orderTotal.Amount - discountAmount.Amount),
            orderTotal.Currency);

        return new ValidateDiscountCodeResult(
            discountCode.Id.Value,
            discountCode.Code,
            discountCode.Type.ToString(),
            discountCode.Value,
            discountAmount.Amount,
            finalTotal.Amount,
            finalTotal.Currency);
    }
}
