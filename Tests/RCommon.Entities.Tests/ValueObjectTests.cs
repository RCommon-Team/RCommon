using FluentAssertions;
using RCommon.Entities;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for ValueObject abstract record.
/// </summary>
public class ValueObjectTests
{
    #region Test Value Objects

    private record Money(decimal Amount, string Currency) : ValueObject;

    private record Address(string Street, string City, string ZipCode) : ValueObject;

    private record EmailAddress(string Value) : ValueObject<string>(Value)
    {
        public static implicit operator EmailAddress(string value) => new(value);
    }

    private record CustomerId(Guid Value) : ValueObject<Guid>(Value)
    {
        public static implicit operator CustomerId(Guid value) => new(value);
    }

    #endregion

    #region Structural Equality Tests

    [Fact]
    public void ValueObject_SameValues_AreEqual()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");
        money1.Should().Be(money2);
    }

    [Fact]
    public void ValueObject_DifferentValues_AreNotEqual()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "USD");
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void ValueObject_DifferentTypes_AreNotEqual()
    {
        var money = new Money(100.00m, "USD");
        var address = new Address("123 Main St", "Springfield", "62701");
        money.Should().NotBe(address);
    }

    [Fact]
    public void ValueObject_SameValues_HaveSameHashCode()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact]
    public void ValueObject_DifferentValues_HaveDifferentHashCode()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "EUR");
        money1.GetHashCode().Should().NotBe(money2.GetHashCode());
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void ValueObject_EqualityOperator_ReturnsTrueForSameValues()
    {
        var money1 = new Money(50.00m, "GBP");
        var money2 = new Money(50.00m, "GBP");
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_InequalityOperator_ReturnsTrueForDifferentValues()
    {
        var money1 = new Money(50.00m, "GBP");
        var money2 = new Money(75.00m, "GBP");
        (money1 != money2).Should().BeTrue();
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void ValueObject_WithExpression_CreatesModifiedCopy()
    {
        var original = new Money(100.00m, "USD");
        var modified = original with { Amount = 200.00m };
        modified.Amount.Should().Be(200.00m);
        modified.Currency.Should().Be("USD");
        original.Amount.Should().Be(100.00m, "original should be unchanged");
    }

    #endregion

    #region Interface Conformance Tests

    [Fact]
    public void ValueObject_ConcreteType_IsAssignableToValueObject()
    {
        var money = new Money(100.00m, "USD");
        money.Should().BeAssignableTo<ValueObject>();
    }

    #endregion

    #region Generic ValueObject<T> Tests

    [Fact]
    public void GenericValueObject_SameValues_AreEqual()
    {
        var email1 = new EmailAddress("user@example.com");
        var email2 = new EmailAddress("user@example.com");
        email1.Should().Be(email2);
    }

    [Fact]
    public void GenericValueObject_DifferentValues_AreNotEqual()
    {
        var email1 = new EmailAddress("user@example.com");
        var email2 = new EmailAddress("other@example.com");
        email1.Should().NotBe(email2);
    }

    [Fact]
    public void GenericValueObject_ImplicitConversionToUnderlyingType()
    {
        var email = new EmailAddress("user@example.com");
        string raw = email;
        raw.Should().Be("user@example.com");
    }

    [Fact]
    public void GenericValueObject_ImplicitConversionFromUnderlyingType()
    {
        EmailAddress email = "user@example.com";
        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void GenericValueObject_ToString_ReturnsUnderlyingValue()
    {
        var email = new EmailAddress("user@example.com");
        email.ToString().Should().Be("user@example.com");
    }

    [Fact]
    public void GenericValueObject_IsAssignableToValueObject()
    {
        var email = new EmailAddress("user@example.com");
        email.Should().BeAssignableTo<ValueObject<string>>();
        email.Should().BeAssignableTo<ValueObject>();
    }

    [Fact]
    public void GenericValueObject_WithGuidValue_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        CustomerId customerId = id;
        Guid raw = customerId;
        raw.Should().Be(id);
    }

    [Fact]
    public void GenericValueObject_SameValues_HaveSameHashCode()
    {
        var email1 = new EmailAddress("user@example.com");
        var email2 = new EmailAddress("user@example.com");
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    #endregion
}
