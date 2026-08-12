namespace ApplyAML;

/// <summary>
/// Defines a strategy for determining whether a value of type <typeparamref name="T"/> can be ignored.
/// </summary>
/// <typeparam name="T">The type of value to evaluate.</typeparam>
public interface IIgnore<T>
{
    /// <summary>
    /// Determines whether the specified value can be ignored.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <returns><c>true</c> if the value can be ignored; otherwise, <c>false</c>.</returns>
    bool CanIgnore(T value);
}

/// <summary>
/// An <see cref="IIgnore{T}"/> implementation that ignores all values unconditionally.
/// </summary>
/// <typeparam name="T">The type of value to evaluate.</typeparam>
public sealed class AlwaysIgnore<T> : IIgnore<T>
{
    /// <summary>The singleton instance.</summary>
    public static readonly AlwaysIgnore<T> Instance = new();
    private AlwaysIgnore() { }

    /// <inheritdoc />
    public bool CanIgnore(T value) => true;
}

/// <summary>
/// An <see cref="IIgnore{T}"/> implementation that never ignores any value.
/// </summary>
/// <typeparam name="T">The type of value to evaluate.</typeparam>
public sealed class NeverIgnore<T> : IIgnore<T>
{
    /// <summary>The singleton instance.</summary>
    public static readonly NeverIgnore<T> Instance = new();
    private NeverIgnore() { }

    /// <inheritdoc />
    public bool CanIgnore(T value) => false;
}
