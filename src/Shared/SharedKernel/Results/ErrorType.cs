namespace MicroServiceSystem.SharedKernel.Results;

public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6,
    Unavailable = 7,
    TooManyRequests = 8
}
