namespace MicroServiceSystem.SharedKernel.Results;

public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    private Error(string code, string description, ErrorType type, IReadOnlyDictionary<string, string[]>? failures = null)
    {
        Code = code;
        Description = description;
        Type = type;
        Failures = failures ?? new Dictionary<string, string[]>();
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public IReadOnlyDictionary<string, string[]> Failures { get; }

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);

    public static Error Unavailable(string code, string description) => new(code, description, ErrorType.Unavailable);

    public static Error TooManyRequests(string code, string description) => new(code, description, ErrorType.TooManyRequests);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error Validation(string code, string description, IReadOnlyDictionary<string, string[]> failures) =>
        new(code, description, ErrorType.Validation, failures);

    /// <summary>
    /// Returns a copy with a culture-specific description while preserving code, type, and failures.
    /// </summary>
    public Error WithDescription(string description) => new(Code, description, Type, Failures);

    public override string ToString() => string.IsNullOrEmpty(Code) ? Description : $"{Code}: {Description}";
}
