namespace MicroServiceSystem.SharedKernel.Primitives;

public class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message) => Code = code;

    public DomainException(string code, string message, Exception innerException)
        : base(message, innerException) => Code = code;

    public string Code { get; } = string.Empty;
}
