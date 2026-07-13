using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Orders;

namespace EventHub.Application.Orders.Commands;

public sealed record ReconcileOrderReservationsCommand(
    OrderId OrderId,
    OrderReservationTransition Transition) : ICommand;

public enum OrderReservationTransition
{
    Commit,
    Release,
}
