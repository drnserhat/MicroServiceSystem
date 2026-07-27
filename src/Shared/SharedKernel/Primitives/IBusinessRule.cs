namespace MicroServiceSystem.SharedKernel.Primitives;

public interface IBusinessRule
{
    string Code { get; }

    string Message { get; }

    bool IsBroken();
}
