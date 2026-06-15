namespace EventHub.Api.Endpoints;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder endpoints);
}
