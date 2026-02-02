using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling;
using RCommon.Persistence.Transactions;
using System.Transactions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class UnitOfWorkFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IGuidGenerator> _mockGuidGenerator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public UnitOfWorkFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockGuidGenerator = new Mock<IGuidGenerator>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockGuidGenerator.Setup(x => x.Create()).Returns(Guid.NewGuid());
    }

    [Fact]
    public void Constructor_WithValidServiceProvider_CreatesInstance()
    {
        // Arrange & Act
        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new UnitOfWorkFactory(null!, _mockGuidGenerator.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Create_WithNoParameters_ReturnsUnitOfWork()
    {
        // Arrange
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(_mockUnitOfWork.Object);
    }

    [Fact]
    public void Create_WithTransactionMode_ReturnsUnitOfWorkWithCorrectMode()
    {
        // Arrange
        var expectedMode = TransactionMode.New;
        _mockUnitOfWork.SetupProperty(x => x.TransactionMode);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        var result = factory.Create(expectedMode);

        // Assert
        result.Should().NotBeNull();
        result.TransactionMode.Should().Be(expectedMode);
    }

    [Fact]
    public void Create_WithTransactionModeAndIsolationLevel_ReturnsUnitOfWork()
    {
        // Arrange
        var expectedMode = TransactionMode.New;
        var expectedIsolationLevel = IsolationLevel.Serializable;
        _mockUnitOfWork.SetupProperty(x => x.TransactionMode);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        var result = factory.Create(expectedMode, expectedIsolationLevel);

        // Assert
        result.Should().NotBeNull();
        result.TransactionMode.Should().Be(expectedMode);
    }

    [Theory]
    [InlineData(TransactionMode.Default)]
    [InlineData(TransactionMode.New)]
    [InlineData(TransactionMode.Supress)]
    public void Create_WithDifferentTransactionModes_SetsCorrectMode(TransactionMode mode)
    {
        // Arrange
        _mockUnitOfWork.SetupProperty(x => x.TransactionMode);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        var result = factory.Create(mode);

        // Assert
        result.TransactionMode.Should().Be(mode);
    }

    [Fact]
    public void Create_CallsServiceProviderGetService()
    {
        // Arrange
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        factory.Create();

        // Assert
        _mockServiceProvider.Verify(x => x.GetService(typeof(IUnitOfWork)), Times.Once);
    }

    [Fact]
    public void Create_WithTransactionMode_CallsServiceProviderGetService()
    {
        // Arrange
        _mockUnitOfWork.SetupProperty(x => x.TransactionMode);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        factory.Create(TransactionMode.New);

        // Assert
        _mockServiceProvider.Verify(x => x.GetService(typeof(IUnitOfWork)), Times.Once);
    }

    [Fact]
    public void Create_WhenServiceProviderReturnsNull_ReturnsNull()
    {
        // Arrange
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns((IUnitOfWork?)null);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleCalls_CreatesNewInstancesEachTime()
    {
        // Arrange
        var mockUnitOfWork1 = new Mock<IUnitOfWork>();
        var mockUnitOfWork2 = new Mock<IUnitOfWork>();

        var callCount = 0;
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(() => callCount++ == 0 ? mockUnitOfWork1.Object : mockUnitOfWork2.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        var result1 = factory.Create();
        var result2 = factory.Create();

        // Assert
        result1.Should().NotBeSameAs(result2);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    public void Create_WithDifferentIsolationLevels_ReturnsUnitOfWork(IsolationLevel isolationLevel)
    {
        // Arrange
        _mockUnitOfWork.SetupProperty(x => x.TransactionMode);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);

        var factory = new UnitOfWorkFactory(_mockServiceProvider.Object, _mockGuidGenerator.Object);

        // Act
        var result = factory.Create(TransactionMode.Default, isolationLevel);

        // Assert
        result.Should().NotBeNull();
    }
}
