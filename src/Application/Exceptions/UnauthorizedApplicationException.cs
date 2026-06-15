namespace EventHub.Application.Exceptions;

public sealed class UnauthorizedApplicationException : ApplicationException
{
    public UnauthorizedApplicationException(string code, string message)
        : base(code, message)
    {
    }
}
