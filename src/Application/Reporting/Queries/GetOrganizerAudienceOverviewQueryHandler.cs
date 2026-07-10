using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;

namespace EventHub.Application.Reporting.Queries;

public sealed class GetOrganizerAudienceOverviewQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IReportingRepository reportingRepository)
    : QueryHandler<GetOrganizerAudienceOverviewQuery, OrganizerAudienceOverviewResult>
{
    public override async Task<Result<OrganizerAudienceOverviewResult>> Handle(
        GetOrganizerAudienceOverviewQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return Error.Unauthorized("UNAUTHORIZED", "Authentication is required.");
        }

        return await reportingRepository.GetOrganizerOverviewAsync(userId.Value, cancellationToken);
    }
}
