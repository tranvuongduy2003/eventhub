using Solution.Api.Http.Binding;
using Solution.Api.Http.Problems;

namespace Solution.Api.Http.Filters;

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
