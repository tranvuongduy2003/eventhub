namespace EventHub.Application.Exceptions;

public class ApplicationException : Exception
{
    public ApplicationException(string message)
        : base(message)
    {
    }

    public ApplicationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
