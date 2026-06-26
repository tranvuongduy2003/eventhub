using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.DiscountCodes.Commands;

public sealed class CreateDiscountCodeCommandHandler(
    IDiscountCodeRepository discountCodeRepository,
    IEventRepository eventRepository,
    IClock clock)
    : CommandHandler<CreateDiscountCodeCommand, CreateDiscountCodeResult>
{
    public override async Task<Result<CreateDiscountCodeResult>> Handle(
        CreateDiscountCodeCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return DiscountCodeErrors.EventNotFound;
        }

        var normalizedCode = DiscountCode.NormalizeCode(command.Code);

        // Check for duplicate code within the event
        var existing = await discountCodeRepository.GetByCodeAsync(
            command.EventId, normalizedCode, cancellationToken);

        if (existing is not null && !existing.DeletedAt.HasValue)
        {
            return DiscountCodeErrors.AlreadyExists;
        }

        try
        {
            var discountCode = DiscountCode.Create(
                command.EventId,
                normalizedCode,
                command.Type,
                command.Value,
                command.StartAt,
                command.EndAt,
                command.UsageCap,
                clock.UtcNow);

            await discountCodeRepository.AddAsync(discountCode, cancellationToken);

            return new CreateDiscountCodeResult(
                discountCode.Id.Value,
                discountCode.EventId,
                discountCode.Code,
                discountCode.Type.ToString(),
                discountCode.Value,
                discountCode.StartAt,
                discountCode.EndAt,
                discountCode.UsageCap,
                discountCode.UsedCount,
                discountCode.CreatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
