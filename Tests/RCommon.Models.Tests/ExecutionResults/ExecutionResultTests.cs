using FluentAssertions;
using RCommon.Models.ExecutionResults;
using Xunit;

namespace RCommon.Models.Tests.ExecutionResults;

/// <summary>
/// Tests for the ExecutionResult base class and its static factory methods.
/// </summary>
public class ExecutionResultTests
{
    [Fact]
    public void Success_ShouldReturnSuccessExecutionResult()
    {
        // Arrange & Act
        var result = ExecutionResult.Success();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Should().BeOfType<SuccessExecutionResult>();
    }

    [Fact]
    public void Success_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var result1 = ExecutionResult.Success();
        var result2 = ExecutionResult.Success();

        // Assert
        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void Failed_WithNoErrors_ShouldReturnFailedExecutionResult()
    {
        // Arrange & Act
        var result = ExecutionResult.Failed();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Should().BeOfType<FailedExecutionResult>();
    }

    [Fact]
    public void Failed_WithNoErrors_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var result1 = ExecutionResult.Failed();
        var result2 = ExecutionResult.Failed();

        // Assert
        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void Failed_WithEnumerableErrors_ShouldReturnFailedExecutionResultWithErrors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var result = ExecutionResult.Failed(errors);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Should().BeOfType<FailedExecutionResult>();
        var failedResult = (FailedExecutionResult)result;
        failedResult.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Failed_WithParamsErrors_ShouldReturnFailedExecutionResultWithErrors()
    {
        // Arrange & Act
        var result = ExecutionResult.Failed("Error 1", "Error 2", "Error 3");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Should().BeOfType<FailedExecutionResult>();
        var failedResult = (FailedExecutionResult)result;
        failedResult.Errors.Should().HaveCount(3);
        failedResult.Errors.Should().Contain("Error 1");
        failedResult.Errors.Should().Contain("Error 2");
        failedResult.Errors.Should().Contain("Error 3");
    }

    [Fact]
    public void Failed_WithEnumerableErrors_ShouldReturnNewInstance()
    {
        // Arrange
        var errors = new List<string> { "Error" };

        // Act
        var result1 = ExecutionResult.Failed(errors);
        var result2 = ExecutionResult.Failed(errors);

        // Assert
        result1.Should().NotBeSameAs(result2);
    }

    [Fact]
    public void Failed_WithParamsErrors_ShouldReturnNewInstance()
    {
        // Arrange & Act
        var result1 = ExecutionResult.Failed("Error");
        var result2 = ExecutionResult.Failed("Error");

        // Assert
        result1.Should().NotBeSameAs(result2);
    }
}
