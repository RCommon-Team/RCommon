using FluentAssertions;
using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;
using Xunit;

namespace RCommon.Models.Tests.Commands;

/// <summary>
/// Tests for the CommandResult generic record.
/// </summary>
public class CommandResultTests
{
    [Fact]
    public void Constructor_WithSuccessExecutionResult_ShouldCreateCommandResult()
    {
        // Arrange
        var executionResult = new SuccessExecutionResult();

        // Act
        var commandResult = new CommandResult<SuccessExecutionResult>(executionResult);

        // Assert
        commandResult.Should().NotBeNull();
        commandResult.Result.Should().BeSameAs(executionResult);
    }

    [Fact]
    public void Constructor_WithFailedExecutionResult_ShouldCreateCommandResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var executionResult = new FailedExecutionResult(errors);

        // Act
        var commandResult = new CommandResult<FailedExecutionResult>(executionResult);

        // Assert
        commandResult.Should().NotBeNull();
        commandResult.Result.Should().BeSameAs(executionResult);
        commandResult.Result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Result_ShouldReturnSuccessStatus_WhenSuccessExecutionResult()
    {
        // Arrange
        var executionResult = new SuccessExecutionResult();
        var commandResult = new CommandResult<SuccessExecutionResult>(executionResult);

        // Act
        var isSuccess = commandResult.Result.IsSuccess;

        // Assert
        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void Result_ShouldReturnFailedStatus_WhenFailedExecutionResult()
    {
        // Arrange
        var executionResult = new FailedExecutionResult(new[] { "Error" });
        var commandResult = new CommandResult<FailedExecutionResult>(executionResult);

        // Act
        var isSuccess = commandResult.Result.IsSuccess;

        // Assert
        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void ShouldImplementICommandResultInterface()
    {
        // Arrange & Act
        var executionResult = new SuccessExecutionResult();
        var commandResult = new CommandResult<SuccessExecutionResult>(executionResult);

        // Assert
        commandResult.Should().BeAssignableTo<ICommandResult<SuccessExecutionResult>>();
    }

    [Fact]
    public void ShouldImplementIModelInterface()
    {
        // Arrange & Act
        var executionResult = new SuccessExecutionResult();
        var commandResult = new CommandResult<SuccessExecutionResult>(executionResult);

        // Assert
        commandResult.Should().BeAssignableTo<IModel>();
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithSameResult_ShouldBeEqual()
    {
        // Arrange
        var executionResult1 = new SuccessExecutionResult();
        var executionResult2 = new SuccessExecutionResult();
        var commandResult1 = new CommandResult<SuccessExecutionResult>(executionResult1);
        var commandResult2 = new CommandResult<SuccessExecutionResult>(executionResult2);

        // Act & Assert
        commandResult1.Should().Be(commandResult2);
        (commandResult1 == commandResult2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithDifferentResults_ShouldNotBeEqual()
    {
        // Arrange
        var executionResult1 = new FailedExecutionResult(new[] { "Error 1" });
        var executionResult2 = new FailedExecutionResult(new[] { "Error 2" });
        var commandResult1 = new CommandResult<FailedExecutionResult>(executionResult1);
        var commandResult2 = new CommandResult<FailedExecutionResult>(executionResult2);

        // Act & Assert
        commandResult1.Should().NotBe(commandResult2);
        (commandResult1 != commandResult2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_TwoEqualInstances_ShouldHaveSameHashCode()
    {
        // Arrange
        var executionResult1 = new SuccessExecutionResult();
        var executionResult2 = new SuccessExecutionResult();
        var commandResult1 = new CommandResult<SuccessExecutionResult>(executionResult1);
        var commandResult2 = new CommandResult<SuccessExecutionResult>(executionResult2);

        // Act
        var hash1 = commandResult1.GetHashCode();
        var hash2 = commandResult2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Constructor_WithIExecutionResultInterface_ShouldWork()
    {
        // Arrange
        IExecutionResult executionResult = ExecutionResult.Success();

        // Act
        var commandResult = new CommandResult<IExecutionResult>(executionResult);

        // Assert
        commandResult.Should().NotBeNull();
        commandResult.Result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithFailedFactoryMethod_ShouldWork()
    {
        // Arrange
        var executionResult = (FailedExecutionResult)ExecutionResult.Failed("Test error");

        // Act
        var commandResult = new CommandResult<FailedExecutionResult>(executionResult);

        // Assert
        commandResult.Should().NotBeNull();
        commandResult.Result.IsSuccess.Should().BeFalse();
        commandResult.Result.Errors.Should().Contain("Test error");
    }
}
