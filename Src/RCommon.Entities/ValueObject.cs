namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base record for value objects. Leverages C# record semantics for automatic
    /// structural equality, immutability, and with-expression support.
    ///
    /// Derive concrete value objects from this type:
    /// <code>
    /// public record Money(decimal Amount, string Currency) : ValueObject;
    /// public record Address(string Street, string City, string ZipCode) : ValueObject;
    /// </code>
    /// </summary>
    public abstract record ValueObject;

    /// <summary>
    /// Abstract base record for single-value wrapper value objects. Provides a typed
    /// <see cref="Value"/> property and implicit conversions to/from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    /// <example>
    /// <code>
    /// public record EmailAddress(string Value) : ValueObject&lt;string&gt;(Value);
    /// public record CustomerId(Guid Value) : ValueObject&lt;Guid&gt;(Value);
    ///
    /// EmailAddress email = "user@example.com";  // implicit from string
    /// string raw = email;                        // implicit to string
    /// </code>
    /// </example>
    public abstract record ValueObject<T>(T Value) : ValueObject
        where T : notnull
    {
        /// <summary>
        /// Implicitly converts a <see cref="ValueObject{T}"/> to its underlying value.
        /// </summary>
        public static implicit operator T(ValueObject<T> valueObject)
            => valueObject.Value;

        /// <inheritdoc/>
        public sealed override string ToString() => Value.ToString() ?? string.Empty;
    }
}
