using EventHub.Application.Common;
using FluentValidation;
using MediatR;

namespace EventHub.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => ToErrorFieldKey(g.Key),
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        var error = Error.Validation(
            "One or more validation errors occurred.",
            errors);

        return ResultFactory.CreateFailure<TResponse>(error);
    }

    private static string ToErrorFieldKey(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "_form";
        }

        if (propertyName.Length == 1)
        {
            return propertyName.ToLowerInvariant();
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
