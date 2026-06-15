namespace EventHub.Application.Common;

internal static class ResultFactory
{
    public static TResponse CreateFailure<TResponse>(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(ResultFactory)
                .GetMethod(
                    nameof(CreateGenericFailure),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(valueType);

            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"Request pipeline expected {nameof(Result)} or {nameof(Result<>)} as response type but got {responseType.Name}.");
    }

    private static Result<T> CreateGenericFailure<T>(Error error) => Result<T>.Failure(error);
}
