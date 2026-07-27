namespace MicroServiceSystem.SharedKernel.Results;

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    internal static Result<TValue> Create(TValue value) => new(value, true, Error.None);

    internal static Result<TValue> Create(Error error) => new(default, false, error);

    public static implicit operator Result<TValue>(TValue value) => Create(value);

    public static implicit operator Result<TValue>(Error error) => Create(error);
}
