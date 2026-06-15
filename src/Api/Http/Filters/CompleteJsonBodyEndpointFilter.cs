using EventHub.Api.Http.Binding;
using EventHub.Api.Http.Problems;

namespace EventHub.Api.Http.Filters;

internal sealed class CompleteJsonBodyEndpointFilter<TBody> : IEndpointFilter
    where TBody : class
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var body = context.Arguments.OfType<TBody>().FirstOrDefault();
        if (JsonBodyBindingValidator.IsIncomplete(body))
        {
            return ValueTask.FromResult<object?>(InvalidRequestProblems.AsResult());
        }

        return next(context);
    }
}
