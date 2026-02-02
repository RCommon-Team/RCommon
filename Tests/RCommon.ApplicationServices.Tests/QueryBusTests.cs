using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Queries;
using RCommon.ApplicationServices.Validation;
using RCommon.Caching;
using RCommon.Models.Queries;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class QueryBusTests
{
    private readonly Mock<ILogger<QueryBus>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IValidationService> _mockValidationService;
    private readonly Mock<IOptions<CqrsValidationOptions>> _mockValidationOptions;
    private readonly Mock<IOptions<CachingOptions>> _mockCachingOptions;

    public QueryBusTests()
    {
        _mockLogger = new Mock<ILogger<QueryBus>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockValidationService = new Mock<IValidationService>();
        _mockValidationOptions = new Mock<IOptions<CqrsValidationOptions>>();
        _mockCachingOptions = new Mock<IOptions<CachingOptions>>();

        // Default setup
        _mockValidationOptions.Setup(x => x.Value).Returns(new CqrsValidationOptions());
        _mockCachingOptions.Setup(x => x.Value).Returns(new CachingOptions { CachingEnabled = false });
    }

    private QueryBus CreateQueryBus()
    {
        return new QueryBus(
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockValidationService.Object,
            _mockValidationOptions.Object,
            _mockCachingOptions.Object);
    }

    [Fact]
    public async Task DispatchQueryAsync_WithValidationEnabled_ValidatesQuery()
    {
        // Arrange
        var validationOptions = new CqrsValidationOptions { ValidateQueries = true };
        _mockValidationOptions.Setup(x => x.Value).Returns(validationOptions);

        var query = new TestQuery();
        var expectedResult = new TestQueryResult { Data = "Test Data" };

        var mockHandler = new Mock<IQueryHandler<TestQuery, TestQueryResult>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(mockHandler.Object);

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<IQuery<TestQueryResult>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var queryBus = CreateQueryBus();

        // Act
        await queryBus.DispatchQueryAsync(query);

        // Assert
        _mockValidationService.Verify(
            x => x.ValidateAsync(It.IsAny<IQuery<TestQueryResult>>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchQueryAsync_WithValidationDisabled_DoesNotValidateQuery()
    {
        // Arrange
        var validationOptions = new CqrsValidationOptions { ValidateQueries = false };
        _mockValidationOptions.Setup(x => x.Value).Returns(validationOptions);

        var query = new TestQuery();
        var expectedResult = new TestQueryResult { Data = "Test Data" };

        var mockHandler = new Mock<IQueryHandler<TestQuery, TestQueryResult>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(mockHandler.Object);

        var queryBus = CreateQueryBus();

        // Act
        await queryBus.DispatchQueryAsync(query);

        // Assert
        _mockValidationService.Verify(
            x => x.ValidateAsync(It.IsAny<IQuery<TestQueryResult>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DispatchQueryAsync_WithRegisteredHandler_ExecutesHandler()
    {
        // Arrange
        var query = new TestQuery();
        var expectedResult = new TestQueryResult { Data = "Test Data" };

        var mockHandler = new Mock<IQueryHandler<TestQuery, TestQueryResult>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(mockHandler.Object);

        var queryBus = CreateQueryBus();

        // Act
        var result = await queryBus.DispatchQueryAsync(query);

        // Assert
        result.Should().BeSameAs(expectedResult);
        result.Data.Should().Be("Test Data");
    }

    [Fact]
    public async Task DispatchQueryAsync_WithCancellationToken_PassesTokenToHandler()
    {
        // Arrange
        var query = new TestQuery();
        var cancellationToken = new CancellationToken();
        var expectedResult = new TestQueryResult { Data = "Test Data" };

        var mockHandler = new Mock<IQueryHandler<TestQuery, TestQueryResult>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(mockHandler.Object);

        var queryBus = CreateQueryBus();

        // Act
        await queryBus.DispatchQueryAsync(query, cancellationToken);

        // Assert
        mockHandler.Verify(
            h => h.HandleAsync(query, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task DispatchQueryAsync_WhenHandlerNotRegistered_ThrowsException()
    {
        // Arrange
        var query = new TestQuery();

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((object?)null);

        var queryBus = CreateQueryBus();

        // Act
        var act = async () => await queryBus.DispatchQueryAsync(query);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DispatchQueryAsync_WithNullValidationOptions_DoesNotValidate()
    {
        // Arrange
        _mockValidationOptions.Setup(x => x.Value).Returns((CqrsValidationOptions)null!);

        var query = new TestQuery();
        var expectedResult = new TestQueryResult { Data = "Test Data" };

        var mockHandler = new Mock<IQueryHandler<TestQuery, TestQueryResult>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(mockHandler.Object);

        var queryBus = CreateQueryBus();

        // Act
        var result = await queryBus.DispatchQueryAsync(query);

        // Assert
        result.Should().BeSameAs(expectedResult);
        _mockValidationService.Verify(
            x => x.ValidateAsync(It.IsAny<IQuery<TestQueryResult>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

// Test query class for testing
public class TestQuery : IQuery<TestQueryResult>
{
}

// Test query result class
public class TestQueryResult
{
    public string Data { get; set; } = string.Empty;
}
