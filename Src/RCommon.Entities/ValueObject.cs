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
}
