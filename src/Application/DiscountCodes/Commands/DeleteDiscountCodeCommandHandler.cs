using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.DiscountCodes;

namespace EventHub.Application.DiscountCodes.Commands;

public sealed class DeleteDiscountCodeCommandHandler(
    IDiscountCodeRepository discountCodeRepository,
    IClock clock)
    : CommandHandler<DeleteDiscountCodeCommand>
{
    public override async Task<Result> Handle(
        DeleteDiscountCodeCommand command,
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

        discountCode.Delete(clock.UtcNow);

        await discountCodeRepository.Update(discountCode, cancellationToken);

        return Result.Success();
    }
}
