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
}
