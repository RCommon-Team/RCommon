using System.Collections.Concurrent;
using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class TypeExtensionsTests
{
    #region Helper Types

    private class ClassWithStringCtor
    {
        public ClassWithStringCtor(string value) { }
    }

    private class ClassWithNoCtor { }

    private interface ITestInterface { }

    private class TestImplementation : ITestInterface { }

    private class UnrelatedClass { }

    #endregion

    #region GetGenericTypeName Tests

    [Fact]
    public void GetGenericTypeName_NonGenericType_ReturnsTypeName()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var result = type.GetGenericTypeName();

        // Assert
        result.Should().Be("String");
    }

    [Fact]
    public void GetGenericTypeName_GenericType_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(List<string>);

        // Act
        var result = type.GetGenericTypeName();

        // Assert
        result.Should().Be("List<String>");
    }

    [Fact]
    public void GetGenericTypeName_MultipleGenericArgs_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(Dictionary<string, int>);

        // Act
        var result = type.GetGenericTypeName();

        // Assert
        result.Should().Be("Dictionary<String,Int32>");
    }

    #endregion

    #region PrettyPrint Tests

    [Fact]
    public void PrettyPrint_SimpleType_ReturnsName()
    {
        // Arrange
        var type = typeof(int);

        // Act
        var result = type.PrettyPrint();

        // Assert
        result.Should().Be("Int32");
    }

    [Fact]
    public void PrettyPrint_GenericType_ReturnsReadableName()
    {
        // Arrange
        var type = typeof(List<string>);

        // Act
        var result = type.PrettyPrint();

        // Assert
        result.Should().Be("List<String>");
    }

    [Fact]
    public void PrettyPrint_SameType_ReturnsCachedResult()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var firstCall = type.PrettyPrint();
        var secondCall = type.PrettyPrint();

        // Assert
        // ReferenceEquals confirms the same cached string instance is returned on subsequent calls
        object.ReferenceEquals(firstCall, secondCall).Should().BeTrue();
    }

    #endregion

    #region GetCacheKey Tests

    [Fact]
    public void GetCacheKey_ReturnsExpectedFormat()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var result = type.GetCacheKey();

        // Assert
        result.Should().Contain("hash:");
        result.Should().StartWith(type.PrettyPrint());
    }

    [Fact]
    public void GetCacheKey_SameType_ReturnsSameKey()
    {
        // Arrange
        var type = typeof(int);

        // Act
        var firstKey = type.GetCacheKey();
        var secondKey = type.GetCacheKey();

        // Assert
        firstKey.Should().Be(secondKey);
    }

    #endregion

    #region HasConstructorParameterOfType Tests

    [Fact]
    public void HasConstructorParameterOfType_WithMatchingParam_ReturnsTrue()
    {
        // Arrange
        var type = typeof(ClassWithStringCtor);

        // Act
        var result = type.HasConstructorParameterOfType(t => t == typeof(string));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasConstructorParameterOfType_NoMatchingParam_ReturnsFalse()
    {
        // Arrange
        var type = typeof(ClassWithNoCtor);

        // Act
        var result = type.HasConstructorParameterOfType(t => t == typeof(string));

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsAssignableTo Tests

    [Fact]
    public void IsAssignableTo_ImplementsInterface_ReturnsTrue()
    {
        // Arrange
        var type = typeof(TestImplementation);

        // Act
        var result = type.IsAssignableTo<ITestInterface>();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAssignableTo_DoesNotImplement_ReturnsFalse()
    {
        // Arrange
        var type = typeof(UnrelatedClass);

        // Act
        var result = type.IsAssignableTo<ITestInterface>();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
