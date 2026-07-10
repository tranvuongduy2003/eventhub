using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Orders;

namespace EventHub.Application.Tickets.Queries;

public sealed class GetOrderTicketsQueryHandler(
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    ITicketRepository ticketRepository)
    : QueryHandler<GetOrderTicketsQuery, GetOrderTicketsResult>
{
    public override async Task<Result<GetOrderTicketsResult>> Handle(
        GetOrderTicketsQuery query,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(OrderId.From(query.OrderId), cancellationToken);
        if (order is null)
        {
            return Error.NotFound("ORDER_NOT_FOUND", "The order was not found.");
        }

        var tickets = await ticketRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var eventAggregate = await eventRepository.GetByIdAsync(order.EventId, cancellationToken);

        return new GetOrderTicketsResult(
            order.Id.Value,
            order.Status.ToString().ToLowerInvariant(),
            TicketProjection.ToResults(tickets, eventAggregate));
    }
}
