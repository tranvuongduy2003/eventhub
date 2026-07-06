using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;

namespace EventHub.Application.Payments.Commands;

public sealed record ConfirmPaymentCommand(string ProviderReference)
    : ICommand<PaymentNotificationResult>, IUnitOfWorkRequest;

public sealed record FailPaymentCommand(string ProviderReference)
    : ICommand<PaymentNotificationResult>, IUnitOfWorkRequest;

public sealed record PaymentNotificationResult(
    int PaymentId,
    int OrderId,
    string PaymentStatus,
    string OrderStatus,
    bool Applied);
