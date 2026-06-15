namespace EventHub.Application.Common;

public enum ErrorType
{
    Validation = 0,
    NotFound = 1,
    Conflict = 2,
    Unauthorized = 3,
    Forbidden = 4,
    Failure = 5,
    BadRequest = 6,
}
