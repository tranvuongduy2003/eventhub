using Solution.Api.Http.Filters;

namespace Solution.Api.Http;

internal static class HttpEndpointExtensions
{
    public static RouteHandlerBuilder RequireCompleteJsonBody<TBody>(this RouteHandlerBuilder builder)
        where TBody : class =>
        builder.AddEndpointFilter<CompleteJsonBodyEndpointFilter<TBody>>();
}
