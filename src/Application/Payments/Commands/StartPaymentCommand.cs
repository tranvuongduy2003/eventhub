using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;

namespace EventHub.Application.Payments.Commands;

public sealed record StartPaymentCommand(
    int OrderId,
    string SuccessUrl,
    string CancelUrl) : ICommand<StartPaymentResult>, IUnitOfWorkRequest;

public sealed record StartPaymentResult(
    int PaymentId,
    int OrderId,
    decimal Amount,
    string Currency,
    string ProviderReference,
    string RedirectUrl);
