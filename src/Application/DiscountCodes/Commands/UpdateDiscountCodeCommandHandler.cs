using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.DiscountCodes.Commands;

public sealed class UpdateDiscountCodeCommandHandler(
    IDiscountCodeRepository discountCodeRepository,
    IClock clock)
    : CommandHandler<UpdateDiscountCodeCommand, UpdateDiscountCodeResult>
{
    public override async Task<Result<UpdateDiscountCodeResult>> Handle(
        UpdateDiscountCodeCommand command,
        CancellationToken cancellationToken)
    {
        var discountCodeId = DiscountCodeId.From(command.DiscountCodeId);

        var discountCode = await discountCodeRepository.GetByIdAsync(discountCodeId, cancellationToken);
        if (discountCode is null)
        {
            return DiscountCodeErrors.NotFound;
        }

        if (discountCode.EventId != command.EventId)
        {
            return DiscountCodeErrors.NotFound;
        }

        try
        {
            discountCode.Edit(
                command.Type,
                command.Value,
                command.StartAt,
                command.EndAt,
                command.UsageCap,
                clock.UtcNow);

            await discountCodeRepository.Update(discountCode, cancellationToken);

            return new UpdateDiscountCodeResult(
                discountCode.Id.Value,
                discountCode.EventId,
                discountCode.Code,
                discountCode.Type.ToString(),
                discountCode.Value,
                discountCode.StartAt,
                discountCode.EndAt,
                discountCode.UsageCap,
                discountCode.UsedCount,
                discountCode.CreatedAt,
                discountCode.UpdatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
