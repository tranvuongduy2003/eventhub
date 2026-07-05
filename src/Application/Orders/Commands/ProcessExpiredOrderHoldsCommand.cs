using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Orders.Commands;

public sealed record ProcessExpiredOrderHoldsCommand : ICommand<ProcessExpiredOrderHoldsResult>;

public sealed record ProcessExpiredOrderHoldsResult(int ExpiredOrderCount, int ReleasedReservationCount);

