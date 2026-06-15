namespace EventHub.Api.Endpoints;

internal sealed class ApiHealthEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetApiHealth")
            .WithTags("Health");
}
