using EventHub.Api.Common;
using EventHub.Application.Common;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace EventHub.Api.Mapping;

public static class ResultHttpExtensions
{
    public static HttpResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : ToProblemResult(result.Error!);

    public static HttpResult ToHttpResult<T>(this Result<T> result, Func<T, HttpResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null
                ? onSuccess(result.Value!)
                : Results.Ok(result.Value);
        }

        return ToProblemResult(result.Error!);
    }

    public static HttpResult ToCreatedHttpResult<T>(this Result<T> result, Func<T, string> locationFactory)
    {
        if (result.IsSuccess)
        {
            return Results.Created(locationFactory(result.Value!), result.Value);
        }

        return ToProblemResult(result.Error!);
    }

    private static HttpResult ToProblemResult(Error error)
    {
        var problem = new ApiProblemDetails
        {
            Type = ProblemDetailsMapping.ToTypeIdentifier(error.Code),
            Title = error.Message,
            Detail = error.Message,
            Status = ProblemDetailsMapping.ToStatusCode(error.Type),
            Code = error.Code,
        };

        if (error.ValidationErrors is not null)
        {
            problem.Extensions["errors"] = error.ValidationErrors;
        }

        if (error.Extensions is not null)
        {
            foreach (var (key, value) in error.Extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        return Results.Problem(problem);
    }
}
