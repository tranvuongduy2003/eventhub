namespace EventHub.Application.Exceptions;

public sealed class ConcurrencyConflictException : ApplicationException
{
    public ConcurrencyConflictException(string message)
        : base("CONCURRENCY_CONFLICT", message)
    {
    }
}
