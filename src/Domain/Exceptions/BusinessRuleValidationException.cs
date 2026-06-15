namespace EventHub.Domain.Exceptions;

public sealed class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string code, string message)
        : base(code, message)
    {
    }
}
