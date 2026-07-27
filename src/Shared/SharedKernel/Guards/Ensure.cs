using System.Runtime.CompilerServices;

namespace MicroServiceSystem.SharedKernel.Guards;

public static class Ensure
{
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : class =>
        value ?? throw new ArgumentNullException(parameterName);

    public static string NotNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;

    public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        value == Guid.Empty
            ? throw new ArgumentException("Value cannot be an empty identifier.", parameterName)
            : value;

    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        value <= 0
            ? throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.")
            : value;

    public static string MaxLength(
        string value,
        int maxLength,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        value.Length > maxLength
            ? throw new ArgumentOutOfRangeException(parameterName, value.Length, $"Value exceeds {maxLength} characters.")
            : value;
}
