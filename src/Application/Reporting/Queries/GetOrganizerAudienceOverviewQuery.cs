using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Reporting.Queries;

public sealed record GetOrganizerAudienceOverviewQuery : IQuery<OrganizerAudienceOverviewResult>;
