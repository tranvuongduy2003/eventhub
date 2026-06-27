using EventHub.Application.Abstractions.Messaging;
using EventHub.Contracts.Events;

namespace EventHub.Application.Events.Queries;

public sealed record GetPublicEventQuery(string Slug) : IQuery<PublicEventResponse>;
