namespace EventHub.Application.Common;

public sealed record Error
{
    public const string ValidationFailedCode = "VALIDATION_FAILED";

    private Error(
        string code,
        string message,
        ErrorType type,
        IReadOnlyDictionary<string, string[]>? validationErrors,
        IReadOnlyDictionary<string, object?>? extensions)
    {
        Code = code;
        Message = message;
        Type = type;
        ValidationErrors = validationErrors;
        Extensions = extensions;
    }

    public string Code { get; }

    public string Message { get; }

    public ErrorType Type { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    public static Error Validation(
        string message,
        IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(ValidationFailedCode, message, ErrorType.Validation, validationErrors, null);

    public static Error BadRequest(string code, string message) =>
        new(code, message, ErrorType.BadRequest, null, null);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation, null, null);

    public static Error Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, object?> extensions) =>
        new(code, message, ErrorType.Validation, null, extensions);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound, null, null);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict, null, null);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized, null, null);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden, null, null);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure, null, null);
}
