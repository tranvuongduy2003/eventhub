using EventHub.Api.Http.Filters;

namespace EventHub.Api.Http;

internal static class HttpEndpointExtensions
{
    public static RouteHandlerBuilder RequireCompleteJsonBody<TBody>(this RouteHandlerBuilder builder)
        where TBody : class =>
        builder.AddEndpointFilter<CompleteJsonBodyEndpointFilter<TBody>>();
}
