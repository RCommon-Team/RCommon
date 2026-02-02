using FluentAssertions;
using RCommon.Models.ExecutionResults;
using Xunit;

namespace RCommon.Models.Tests.ExecutionResults;

/// <summary>
/// Tests for the FailedExecutionResult record.
/// </summary>
public class FailedExecutionResultTests
{
    [Fact]
    public void Constructor_WithEmptyErrors_ShouldCreateFailedResult()
    {
        // Arrange & Act
        var result = new FailedExecutionResult(Enumerable.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithErrors_ShouldCreateFailedResultWithErrors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var result = new FailedExecutionResult(errors);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void Constructor_WithNullErrors_ShouldCreateFailedResultWithEmptyErrors()
    {
        // Arrange & Act
        var result = new FailedExecutionResult(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void IsSuccess_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var result = new FailedExecutionResult(new[] { "Error" });

        // Act
        var isSuccess = result.IsSuccess;

        // Assert
        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void Errors_ShouldBeReadOnly()
    {
        // Arrange
        var originalErrors = new List<string> { "Error 1" };
        var result = new FailedExecutionResult(originalErrors);

        // Act
        originalErrors.Add("Error 2");

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().NotContain("Error 2");
    }

    [Fact]
    public void ToString_WithNoErrors_ShouldReturnDefaultMessage()
    {
        // Arrange
        var result = new FailedExecutionResult(Enumerable.Empty<string>());

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Failed execution");
    }

    [Fact]
    public void ToString_WithSingleError_ShouldReturnFormattedMessage()
    {
        // Arrange
        var result = new FailedExecutionResult(new[] { "Something went wrong" });

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Failed execution due to: Something went wrong");
    }

    [Fact]
    public void ToString_WithMultipleErrors_ShouldReturnFormattedMessage()
    {
        // Arrange
        var result = new FailedExecutionResult(new[] { "Error 1", "Error 2", "Error 3" });

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Failed execution due to: Error 1, Error 2, Error 3");
    }

    [Fact]
    public void ShouldImplementIExecutionResult()
    {
        // Arrange & Act
        var result = new FailedExecutionResult(Enumerable.Empty<string>());

        // Assert
        result.Should().BeAssignableTo<IExecutionResult>();
    }

    [Fact]
    public void ShouldInheritFromExecutionResult()
    {
        // Arrange & Act
        var result = new FailedExecutionResult(Enumerable.Empty<string>());

        // Assert
        result.Should().BeAssignableTo<ExecutionResult>();
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithSameErrorsCollection_ShouldNotBeEqual_DueToReferenceEquality()
    {
        // Arrange
        // Note: Records with collection properties use reference equality for the collections,
        // so two FailedExecutionResult instances with identical but separately created collections
        // will NOT be equal. This is the expected behavior.
        var result1 = new FailedExecutionResult(new[] { "Error" });
        var result2 = new FailedExecutionResult(new[] { "Error" });

        // Act & Assert
        // These are NOT equal because the Errors collections are different object references
        result1.Should().NotBe(result2);
        (result1 == result2).Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithSameErrorsCollection_ShouldHaveEquivalentContent()
    {
        // Arrange
        var result1 = new FailedExecutionResult(new[] { "Error" });
        var result2 = new FailedExecutionResult(new[] { "Error" });

        // Act & Assert
        // While record equality fails due to reference equality of collections,
        // the content should be equivalent
        result1.Errors.Should().BeEquivalentTo(result2.Errors);
        result1.IsSuccess.Should().Be(result2.IsSuccess);
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithDifferentErrors_ShouldNotBeEqual()
    {
        // Arrange
        var result1 = new FailedExecutionResult(new[] { "Error 1" });
        var result2 = new FailedExecutionResult(new[] { "Error 2" });

        // Act & Assert
        result1.Should().NotBe(result2);
        (result1 != result2).Should().BeTrue();
    }

    [Theory]
    [InlineData("Validation failed")]
    [InlineData("Access denied")]
    [InlineData("Resource not found")]
    public void Constructor_WithVariousErrorMessages_ShouldStoreCorrectly(string errorMessage)
    {
        // Arrange & Act
        var result = new FailedExecutionResult(new[] { errorMessage });

        // Assert
        result.Errors.Should().ContainSingle().Which.Should().Be(errorMessage);
    }

    [Fact]
    public void Constructor_WithLargeNumberOfErrors_ShouldHandleCorrectly()
    {
        // Arrange
        var errors = Enumerable.Range(1, 100).Select(i => $"Error {i}").ToList();

        // Act
        var result = new FailedExecutionResult(errors);

        // Assert
        result.Errors.Should().HaveCount(100);
    }
}
