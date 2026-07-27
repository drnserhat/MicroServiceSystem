namespace MicroServiceSystem.SharedKernel.Results;

public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper) =>
        result.IsSuccess ? Result.Success(mapper(result.Value)) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper) =>
        result.IsSuccess ? Result.Success(await mapper(result.Value)) : Result.Failure<TOut>(result.Error);

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder) =>
        result.IsSuccess ? binder(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder) =>
        result.IsSuccess ? await binder(result.Value) : Result.Failure<TOut>(result.Error);

    public static Result Tap(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);

    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess() : onFailure(result.Error);

    public static Result<TValue> ToResult<TValue>(this TValue? value, Error error)
        where TValue : class =>
        value is null ? Result.Failure<TValue>(error) : Result.Success(value);
}
