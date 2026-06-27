namespace EventHub.Application.Common;

public sealed record EventFilter(
    string? Search,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? Location);
