using FluentAssertions;
using RCommon.Models.ExecutionResults;
using Xunit;

namespace RCommon.Models.Tests.ExecutionResults;

/// <summary>
/// Tests for the SuccessExecutionResult record.
/// </summary>
public class SuccessExecutionResultTests
{
    [Fact]
    public void Constructor_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = new SuccessExecutionResult();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var result = new SuccessExecutionResult();

        // Act
        var isSuccess = result.IsSuccess;

        // Assert
        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnExpectedMessage()
    {
        // Arrange
        var result = new SuccessExecutionResult();

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Successful execution");
    }

    [Fact]
    public void ShouldImplementIExecutionResult()
    {
        // Arrange & Act
        var result = new SuccessExecutionResult();

        // Assert
        result.Should().BeAssignableTo<IExecutionResult>();
    }

    [Fact]
    public void ShouldInheritFromExecutionResult()
    {
        // Arrange & Act
        var result = new SuccessExecutionResult();

        // Assert
        result.Should().BeAssignableTo<ExecutionResult>();
    }

    [Fact]
    public void RecordEquality_TwoInstances_ShouldBeEqual()
    {
        // Arrange
        var result1 = new SuccessExecutionResult();
        var result2 = new SuccessExecutionResult();

        // Act & Assert
        result1.Should().Be(result2);
        (result1 == result2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_TwoEqualInstances_ShouldHaveSameHashCode()
    {
        // Arrange
        var result1 = new SuccessExecutionResult();
        var result2 = new SuccessExecutionResult();

        // Act
        var hash1 = result1.GetHashCode();
        var hash2 = result2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }
}
