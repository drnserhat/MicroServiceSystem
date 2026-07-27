using System.Reflection;

namespace MicroServiceSystem.SharedKernel.Primitives;

public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>, IComparable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<IReadOnlyDictionary<int, TEnum>> Members = new(CreateMembers);

    protected Enumeration(int value, string name)
    {
        Value = value;
        Name = name;
    }

    public int Value { get; }

    public string Name { get; }

    public static IReadOnlyCollection<TEnum> All => Members.Value.Values.ToList().AsReadOnly();

    public static TEnum? FromValue(int value) => Members.Value.GetValueOrDefault(value);

    public static TEnum? FromName(string name) =>
        Members.Value.Values.SingleOrDefault(member => string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool Equals(Enumeration<TEnum>? other) =>
        other is not null && GetType() == other.GetType() && Value == other.Value;

    public override bool Equals(object? obj) => obj is Enumeration<TEnum> enumeration && Equals(enumeration);

    public override int GetHashCode() => HashCode.Combine(GetType(), Value);

    public int CompareTo(Enumeration<TEnum>? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override string ToString() => Name;

    public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right) => Equals(left, right);

    public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right) => !Equals(left, right);

    public static bool operator <(Enumeration<TEnum>? left, Enumeration<TEnum>? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(Enumeration<TEnum>? left, Enumeration<TEnum>? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(Enumeration<TEnum>? left, Enumeration<TEnum>? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(Enumeration<TEnum>? left, Enumeration<TEnum>? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;

    private static IReadOnlyDictionary<int, TEnum> CreateMembers() =>
        typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TEnum))
            .Select(field => (TEnum)field.GetValue(null)!)
            .ToDictionary(member => member.Value);
}
