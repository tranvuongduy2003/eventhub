namespace EventHub.Application.Common;

public interface IResult
{
    bool IsSuccess { get; }

    bool IsFailure { get; }

    Error? Error { get; }
}
