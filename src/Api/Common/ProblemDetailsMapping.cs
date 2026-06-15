using EventHub.Application.Common;

namespace EventHub.Api.Common;

internal static class ProblemDetailsMapping
{
    public const string DefaultType = "about:blank";

    public static int ToStatusCode(ErrorType type) =>
        type switch
        {
            ErrorType.BadRequest => StatusCodes.Status400BadRequest,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

    public static string ToTypeIdentifier(string errorCode) =>
        $"/errors/{errorCode.ToLowerInvariant()}";
}
