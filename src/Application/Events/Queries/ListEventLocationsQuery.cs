using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Events.Queries;

public sealed record ListEventLocationsQuery : IQuery<List<string>>;
