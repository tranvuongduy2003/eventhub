namespace EventHub.Application.Exceptions;

public sealed class NotFoundException : ApplicationException
{
    public NotFoundException(string code, string message)
        : base(code, message)
    {
    }
}
