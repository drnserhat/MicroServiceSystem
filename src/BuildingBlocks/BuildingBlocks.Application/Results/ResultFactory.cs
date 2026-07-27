using System.Collections.Concurrent;
using System.Reflection;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Application.Results;

/// <summary>
/// Produces a failed <see cref="Result"/> for an arbitrary pipeline response type so that behaviors
/// can short circuit without throwing for expected application failures.
/// </summary>
public static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> FailureFactories = new();

    public static bool IsResultType(Type responseType) =>
        responseType == typeof(Result) || IsGenericResult(responseType);

    public static TResponse CreateFailure<TResponse>(Error error)
    {
        Type responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (IsGenericResult(responseType))
        {
            Func<Error, object> factory = FailureFactories.GetOrAdd(responseType, CreateGenericFailureFactory);
            return (TResponse)factory(error);
        }

        throw new InvalidOperationException(
            $"Response type '{responseType.FullName}' is not a Result and cannot represent a failure.");
    }

    private static bool IsGenericResult(Type responseType) =>
        responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>);

    private static Func<Error, object> CreateGenericFailureFactory(Type responseType)
    {
        Type valueType = responseType.GetGenericArguments()[0];

        MethodInfo failureMethod = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
            .MakeGenericMethod(valueType);

        return error => failureMethod.Invoke(null, [error])!;
    }
}
