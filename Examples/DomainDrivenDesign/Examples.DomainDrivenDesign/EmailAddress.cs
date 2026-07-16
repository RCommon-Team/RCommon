using RCommon.Entities;

namespace Examples.DomainDrivenDesign;

/// <summary>
/// A value object wrapping a single string. Two EmailAddress instances with the same Value are
/// equal by RCommon.Entities.ValueObject's record-based structural equality -- no Equals/GetHashCode
/// override needed.
/// </summary>
public record EmailAddress(string Value) : ValueObject<string>(Value)
{
    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
        {
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));
        }

        return new EmailAddress(value);
    }
}
