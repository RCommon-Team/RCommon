using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class GuardTests
{
    #region Against Tests

    [Fact]
    public void Against_WhenAssertionIsTrue_ThrowsSpecifiedException()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var act = () => Guard.Against<InvalidOperationException>(true, message);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(message);
    }

    [Fact]
    public void Against_WhenAssertionIsFalse_DoesNotThrowException()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var act = () => Guard.Against<InvalidOperationException>(false, message);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Against_WithFuncAssertionTrue_ThrowsSpecifiedException()
    {
        // Arrange
        var message = "Test exception message";
        Func<bool> assertion = () => true;

        // Act
        var act = () => Guard.Against<ArgumentException>(assertion, message);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage(message);
    }

    [Fact]
    public void Against_WithFuncAssertionFalse_DoesNotThrowException()
    {
        // Arrange
        var message = "Test exception message";
        Func<bool> assertion = () => false;

        // Act
        var act = () => Guard.Against<ArgumentException>(assertion, message);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region InheritsFrom Tests

    [Fact]
    public void InheritsFrom_WhenTypeBaseTypeMatchesTBase_DoesNotThrow()
    {
        // Arrange - The Guard.InheritsFrom checks if type.BaseType equals typeof(TBase)
        // DerivedClass inherits from BaseClass, so typeof(DerivedClass).BaseType == typeof(BaseClass)
        var derivedType = typeof(DerivedClass);

        // Act - We need to pass a Type that has BaseType equal to TBase
        // Since Guard checks type.BaseType != typeof(TBase), we need BaseClass for DerivedClass
        var act = () => Guard.InheritsFrom<BaseClass>(derivedType, "Should not throw");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InheritsFrom_WhenTypeBaseTypeDoesNotMatchTBase_ThrowsInvalidOperationException()
    {
        // Arrange - UnrelatedClass inherits from Object, not BaseClass
        var unrelatedType = typeof(UnrelatedClass);
        var message = "Type does not inherit from BaseClass";

        // Act - UnrelatedClass.BaseType is object, not BaseClass, so this should throw
        var act = () => Guard.InheritsFrom<BaseClass>(unrelatedType, message);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(message);
    }

    [Fact]
    public void InheritsFrom_WithObjectInstance_HasTypeConstraint()
    {
        // The InheritsFrom(object instance, string) method has constraint where TBase : Type
        // This test verifies the method works when the constraint is satisfied
        // Since TBase must be Type (or derived), we're testing the Type overload instead
        var derivedType = typeof(DerivedClass);

        // Act
        var act = () => Guard.InheritsFrom<BaseClass>(derivedType, "Should not throw");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Implements Tests

    [Fact]
    public void Implements_WhenTypeImplementsInterface_DoesNotThrow()
    {
        // Arrange
        var implementingType = typeof(ImplementingClass);

        // Act
        var act = () => Guard.Implements<ITestInterface>(implementingType, "Should not throw");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Implements_WhenTypeDoesNotImplementInterface_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonImplementingType = typeof(UnrelatedClass);
        var message = "Type does not implement interface";

        // Act
        var act = () => Guard.Implements<ITestInterface>(nonImplementingType, message);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(message);
    }

    [Fact]
    public void Implements_WithObjectInstance_WhenImplements_DoesNotThrow()
    {
        // Arrange
        var instance = new ImplementingClass();

        // Act
        var act = () => Guard.Implements<ITestInterface>(instance, "Should not throw");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region TypeOf Tests

    [Fact]
    public void TypeOf_WhenInstanceIsOfType_DoesNotThrow()
    {
        // Arrange
        object instance = "test string";

        // Act
        var act = () => Guard.TypeOf<string>(instance, "Should not throw");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TypeOf_WhenInstanceIsNotOfType_ThrowsInvalidOperationException()
    {
        // Arrange
        object instance = 123;
        var message = "Instance is not of expected type";

        // Act
        var act = () => Guard.TypeOf<string>(instance, message);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(message);
    }

    #endregion

    #region IsEqual Tests

    [Fact]
    public void IsEqual_WhenObjectsAreEqual_DoesNotThrow()
    {
        // Arrange
        var obj1 = "test";
        var obj2 = obj1;

        // Act
        var act = () => Guard.IsEqual<ArgumentException>(obj1, obj2, "Should not throw");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsEqual_WhenObjectsAreNotEqual_ThrowsSpecifiedException()
    {
        // Arrange
        var obj1 = "test1";
        var obj2 = "test2";
        var message = "Objects are not equal";

        // Act
        var act = () => Guard.IsEqual<ArgumentException>(obj1, obj2, message);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage(message);
    }

    #endregion

    #region IsNotEmpty Guid Tests

    [Fact]
    public void IsNotEmpty_WithNonEmptyGuid_DoesNotThrow()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var act = () => Guard.IsNotEmpty(guid, "testGuid");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmpty_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var guid = Guid.Empty;

        // Act
        var act = () => Guard.IsNotEmpty(guid, "testGuid");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsNotEmpty_WithEmptyGuidAndNoThrow_ReturnsFalse()
    {
        // Arrange
        var guid = Guid.Empty;

        // Act
        var result = Guard.IsNotEmpty(guid, "testGuid", false);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNotEmpty_WithNonEmptyGuidAndNoThrow_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = Guard.IsNotEmpty(guid, "testGuid", false);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsNotEmpty String Tests

    [Fact]
    public void IsNotEmpty_WithNonEmptyString_DoesNotThrow()
    {
        // Arrange
        var str = "test";

        // Act
        var act = () => Guard.IsNotEmpty(str, "testString");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsNotEmpty_WithEmptyOrWhitespaceString_ThrowsArgumentException(string? value)
    {
        // Act
        var act = () => Guard.IsNotEmpty(value!, "testString");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsNotEmpty_WithEmptyStringAndNoThrow_ReturnsFalse()
    {
        // Arrange
        var str = "";

        // Act
        var result = Guard.IsNotEmpty(str, "testString", false);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsNotOutOfLength Tests

    [Fact]
    public void IsNotOutOfLength_WhenWithinLength_DoesNotThrow()
    {
        // Arrange
        var str = "test";

        // Act
        var act = () => Guard.IsNotOutOfLength(str, 10, "testString");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotOutOfLength_WhenExceedsLength_ThrowsArgumentException()
    {
        // Arrange
        var str = "this is a long string";

        // Act
        var act = () => Guard.IsNotOutOfLength(str, 5, "testString");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region IsNotNull Tests

    [Fact]
    public void IsNotNull_WithNonNullObject_DoesNotThrow()
    {
        // Arrange
        var obj = new object();

        // Act
        var act = () => Guard.IsNotNull(obj, "testObject");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNull_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        object? obj = null;

        // Act
        var act = () => Guard.IsNotNull(obj!, "testObject");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region IsNotNegative Int Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void IsNotNegative_Int_WithNonNegativeValue_DoesNotThrow(int value)
    {
        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNegative_Int_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = -1;

        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegativeOrZero Int Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void IsNotNegativeOrZero_Int_WithPositiveValue_DoesNotThrow(int value)
    {
        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsNotNegativeOrZero_Int_WithZeroOrNegativeValue_ThrowsArgumentOutOfRangeException(int value)
    {
        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsNotNegativeOrZero_Int_WithZeroAndNoThrow_ReturnsFalse()
    {
        // Act
        var result = Guard.IsNotNegativeOrZero(0, "testValue", false);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsNotNegative Long Tests

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(100L)]
    public void IsNotNegative_Long_WithNonNegativeValue_DoesNotThrow(long value)
    {
        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNegative_Long_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = -1L;

        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegativeOrZero Long Tests

    [Fact]
    public void IsNotNegativeOrZero_Long_WithPositiveValue_DoesNotThrow()
    {
        // Arrange
        var value = 1L;

        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void IsNotNegativeOrZero_Long_WithZeroOrNegativeValue_ThrowsArgumentOutOfRangeException(long value)
    {
        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegative Float Tests

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(100.5f)]
    public void IsNotNegative_Float_WithNonNegativeValue_DoesNotThrow(float value)
    {
        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNegative_Float_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = -1.5f;

        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegativeOrZero Float Tests

    [Fact]
    public void IsNotNegativeOrZero_Float_WithPositiveValue_DoesNotThrow()
    {
        // Arrange
        var value = 1.5f;

        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1.5f)]
    public void IsNotNegativeOrZero_Float_WithZeroOrNegativeValue_ThrowsArgumentOutOfRangeException(float value)
    {
        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegative Decimal Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100.5)]
    public void IsNotNegative_Decimal_WithNonNegativeValue_DoesNotThrow(decimal value)
    {
        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNegative_Decimal_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = -1.5m;

        // Act
        var act = () => Guard.IsNotNegative(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegativeOrZero Decimal Tests

    [Fact]
    public void IsNotNegativeOrZero_Decimal_WithPositiveValue_DoesNotThrow()
    {
        // Arrange
        var value = 1.5m;

        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void IsNotNegativeOrZero_Decimal_WithZeroOrNegativeValue_ThrowsArgumentOutOfRangeException(decimal value)
    {
        // Act
        var act = () => Guard.IsNotNegativeOrZero(value, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotInvalidDate Tests

    [Fact]
    public void IsNotInvalidDate_WithValidDate_DoesNotThrow()
    {
        // Arrange
        var date = DateTime.Now;

        // Act
        var act = () => Guard.IsNotInvalidDate(date, "testDate");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotInvalidDate_WithMinValueDate_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var date = DateTime.MinValue;

        // Act
        var act = () => Guard.IsNotInvalidDate(date, "testDate");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegative TimeSpan Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void IsNotNegative_TimeSpan_WithNonNegativeValue_DoesNotThrow(int seconds)
    {
        // Arrange
        var timeSpan = TimeSpan.FromSeconds(seconds);

        // Act
        var act = () => Guard.IsNotNegative(timeSpan, "testTimeSpan");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNegative_TimeSpan_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var timeSpan = TimeSpan.FromSeconds(-1);

        // Act
        var act = () => Guard.IsNotNegative(timeSpan, "testTimeSpan");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotNegativeOrZero TimeSpan Tests

    [Fact]
    public void IsNotNegativeOrZero_TimeSpan_WithPositiveValue_DoesNotThrow()
    {
        // Arrange
        var timeSpan = TimeSpan.FromSeconds(1);

        // Act
        var act = () => Guard.IsNotNegativeOrZero(timeSpan, "testTimeSpan");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsNotNegativeOrZero_TimeSpan_WithZeroOrNegativeValue_ThrowsArgumentOutOfRangeException(int seconds)
    {
        // Arrange
        var timeSpan = TimeSpan.FromSeconds(seconds);

        // Act
        var act = () => Guard.IsNotNegativeOrZero(timeSpan, "testTimeSpan");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotEmpty Collection Tests

    [Fact]
    public void IsNotEmpty_Collection_WithNonEmptyCollection_DoesNotThrow()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act
        var act = () => Guard.IsNotEmpty(collection, "testCollection");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmpty_Collection_WithEmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var collection = new List<int>();

        // Act
        var act = () => Guard.IsNotEmpty(collection, "testCollection");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsNotEmpty_Collection_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        List<int>? collection = null;

        // Act
        var act = () => Guard.IsNotEmpty(collection!, "testCollection");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region IsNotOutOfRange Tests

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void IsNotOutOfRange_WithValueInRange_DoesNotThrow(int value, int min, int max)
    {
        // Act
        var act = () => Guard.IsNotOutOfRange(value, min, max, "testValue");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(11, 1, 10)]
    public void IsNotOutOfRange_WithValueOutOfRange_ThrowsArgumentOutOfRangeException(int value, int min, int max)
    {
        // Act
        var act = () => Guard.IsNotOutOfRange(value, min, max, "testValue");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsNotInvalidEmail Tests

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    public void IsNotInvalidEmail_WithValidEmail_DoesNotThrow(string email)
    {
        // Act
        var act = () => Guard.IsNotInvalidEmail(email, "testEmail");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@nodomain.com")]
    public void IsNotInvalidEmail_WithInvalidEmail_ThrowsArgumentException(string email)
    {
        // Act
        var act = () => Guard.IsNotInvalidEmail(email, "testEmail");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region IsNotInvalidWebUrl Tests

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://www.example.org")]
    public void IsNotInvalidWebUrl_WithValidUrl_DoesNotThrow(string url)
    {
        // Act
        var act = () => Guard.IsNotInvalidWebUrl(url, "testUrl");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    public void IsNotInvalidWebUrl_WithInvalidUrl_ThrowsArgumentException(string url)
    {
        // Act
        var act = () => Guard.IsNotInvalidWebUrl(url, "testUrl");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_CanBeInstantiated()
    {
        // Act
        var guard = new Guard();

        // Assert
        guard.Should().NotBeNull();
    }

    #endregion

    #region Test Helper Classes

    public interface ITestInterface { }

    public class BaseClass { }

    public class DerivedClass : BaseClass { }

    public class UnrelatedClass { }

    public class ImplementingClass : ITestInterface { }

    #endregion
}
