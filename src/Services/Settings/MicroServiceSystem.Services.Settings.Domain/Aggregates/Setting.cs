using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;
namespace MicroServiceSystem.Services.Settings.Domain.Aggregates;
public sealed class Setting : TenantAggregateRoot<Guid>
{
    private Setting() { } private Setting(Guid id,string key,string value):base(id){Key=key;Value=value;}
    public string Key { get; private set; }=string.Empty; public string Value { get; private set; }=string.Empty;
    public static Setting Create(string key,string value){Ensure.NotNullOrWhiteSpace(key);Ensure.NotNullOrWhiteSpace(value);return new(Guid.CreateVersion7(),key.Trim(),value);}
    public void SetValue(string value){Ensure.NotNullOrWhiteSpace(value);Value=value;}
}
