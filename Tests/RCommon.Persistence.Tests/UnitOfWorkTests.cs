using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling;
using RCommon.Persistence.Transactions;
using System.Transactions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class UnitOfWorkTests
{
    private readonly Mock<ILogger<UnitOfWork>> _mockLogger;
    private readonly Mock<IGuidGenerator> _mockGuidGenerator;
    private readonly Mock<IOptions<UnitOfWorkSettings>> _mockOptions;
    private readonly UnitOfWorkSettings _settings;
    private readonly Guid _expectedTransactionId;

    public UnitOfWorkTests()
    {
        _mockLogger = new Mock<ILogger<UnitOfWork>>();
        _mockGuidGenerator = new Mock<IGuidGenerator>();
        _mockOptions = new Mock<IOptions<UnitOfWorkSettings>>();
        _settings = new UnitOfWorkSettings();
        _expectedTransactionId = Guid.NewGuid();

        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockGuidGenerator.Setup(x => x.Create()).Returns(_expectedTransactionId);
    }

    [Fact]
    public void Constructor_WithSettings_CreatesInstanceWithCorrectDefaults()
    {
        // Arrange & Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Assert
        unitOfWork.Should().NotBeNull();
        unitOfWork.TransactionId.Should().Be(_expectedTransactionId);
        unitOfWork.TransactionMode.Should().Be(TransactionMode.Default);
        unitOfWork.IsolationLevel.Should().Be(_settings.DefaultIsolation);
        unitOfWork.AutoComplete.Should().Be(_settings.AutoCompleteScope);
        unitOfWork.State.Should().Be(UnitOfWorkState.Created);
    }

    [Fact]
    public void Constructor_WithExplicitTransactionMode_CreatesInstanceWithCorrectSettings()
    {
        // Arrange
        var transactionMode = TransactionMode.New;
        var isolationLevel = IsolationLevel.Serializable;

        // Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, transactionMode, isolationLevel);

        // Assert
        unitOfWork.TransactionMode.Should().Be(transactionMode);
        unitOfWork.IsolationLevel.Should().Be(isolationLevel);
        unitOfWork.AutoComplete.Should().BeFalse();
        unitOfWork.State.Should().Be(UnitOfWorkState.Created);
    }

    [Fact]
    public void Constructor_GeneratesTransactionId()
    {
        // Arrange & Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Assert
        _mockGuidGenerator.Verify(x => x.Create(), Times.Once);
        unitOfWork.TransactionId.Should().Be(_expectedTransactionId);
    }

    [Fact]
    public void TransactionMode_CanBeSet()
    {
        // Arrange
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Act
        unitOfWork.TransactionMode = TransactionMode.New;

        // Assert
        unitOfWork.TransactionMode.Should().Be(TransactionMode.New);
    }

    [Fact]
    public void IsolationLevel_CanBeSet()
    {
        // Arrange
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Act
        unitOfWork.IsolationLevel = IsolationLevel.Serializable;

        // Assert
        unitOfWork.IsolationLevel.Should().Be(IsolationLevel.Serializable);
    }

    [Fact]
    public void Commit_WhenCalled_CompletesUnitOfWork()
    {
        // Arrange
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Act
        unitOfWork.Commit();

        // Assert
        unitOfWork.State.Should().Be(UnitOfWorkState.Completed);
    }

    [Fact]
    public void Commit_WhenAlreadyCompleted_ThrowsUnitOfWorkException()
    {
        // Arrange
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);
        unitOfWork.Commit();

        // Act
        var action = () => unitOfWork.Commit();

        // Assert
        action.Should().Throw<UnitOfWorkException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void Dispose_WhenNotCommitted_RollsBack()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Act
        unitOfWork.Dispose();

        // Assert - the unit of work should be disposed
        unitOfWork.State.Should().Be(UnitOfWorkState.Disposed);
    }

    [Fact]
    public void Dispose_WhenCommitted_DisposesGracefully()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);
        unitOfWork.Commit();

        // Act
        unitOfWork.Dispose();

        // Assert
        unitOfWork.State.Should().Be(UnitOfWorkState.Disposed);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);
        unitOfWork.Commit();

        // Act
        var action = () =>
        {
            unitOfWork.Dispose();
            unitOfWork.Dispose();
        };

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithAutoComplete_CommitsAutomatically()
    {
        // Arrange
        _settings.AutoCompleteScope = true;
        _mockOptions.Setup(x => x.Value).Returns(_settings);

        var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Act
        unitOfWork.Dispose();

        // Assert
        unitOfWork.State.Should().Be(UnitOfWorkState.Disposed);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    public void Constructor_WithDifferentIsolationLevels_SetsCorrectly(IsolationLevel isolationLevel)
    {
        // Arrange & Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object,
            TransactionMode.Default, isolationLevel);

        // Assert
        unitOfWork.IsolationLevel.Should().Be(isolationLevel);
    }

    [Theory]
    [InlineData(TransactionMode.Default)]
    [InlineData(TransactionMode.New)]
    [InlineData(TransactionMode.Supress)]
    public void Constructor_WithDifferentTransactionModes_SetsCorrectly(TransactionMode transactionMode)
    {
        // Arrange & Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object,
            transactionMode, IsolationLevel.ReadCommitted);

        // Assert
        unitOfWork.TransactionMode.Should().Be(transactionMode);
    }

    [Fact]
    public void State_InitiallyCreated_ReturnsCreatedState()
    {
        // Arrange & Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Assert
        unitOfWork.State.Should().Be(UnitOfWorkState.Created);
    }

    [Fact]
    public void AutoComplete_FromSettings_ReturnsSettingsValue()
    {
        // Arrange
        _settings.AutoCompleteScope = true;

        // Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockOptions.Object);

        // Assert
        unitOfWork.AutoComplete.Should().BeTrue();
    }

    [Fact]
    public void AutoComplete_WithExplicitConstructor_ReturnsFalse()
    {
        // Arrange & Act
        using var unitOfWork = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object,
            TransactionMode.Default, IsolationLevel.ReadCommitted);

        // Assert
        unitOfWork.AutoComplete.Should().BeFalse();
    }
}
