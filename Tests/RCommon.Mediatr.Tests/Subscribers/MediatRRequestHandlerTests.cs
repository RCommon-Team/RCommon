using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Mediator.Subscribers;
using RCommon.MediatR.Subscribers;
using Xunit;

namespace RCommon.Mediatr.Tests.Subscribers;

public class MediatRRequestHandlerTests
{
    #region MediatRRequestHandler<T, TRequest> Constructor Tests

    [Fact]
    public void MediatRRequestHandler_Constructor_WithValidServiceProvider_CreatesInstance()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatRRequestHandler<TestAppRequest, MediatRRequest<TestAppRequest>>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void MediatRRequestHandler_ImplementsIRequestHandler()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatRRequestHandler<TestAppRequest, MediatRRequest<TestAppRequest>>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().BeAssignableTo<IRequestHandler<MediatRRequest<TestAppRequest>>>();
    }

    #endregion

    #region MediatRRequestHandler<T, TRequest> Handle Tests

    [Fact]
    public async Task MediatRRequestHandler_Handle_ResolvesHandlerFromServiceProvider()
    {
        // Arrange
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequest>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequest>)))
            .Returns(mockRequestHandler.Object);

        var handler = new MediatRRequestHandler<TestAppRequest, MediatRRequest<TestAppRequest>>(
            mockServiceProvider.Object);

        var request = new MediatRRequest<TestAppRequest>(new TestAppRequest { Data = "Test" });

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockServiceProvider.Verify(x => x.GetService(typeof(IAppRequestHandler<TestAppRequest>)), Times.Once);
    }

    [Fact]
    public async Task MediatRRequestHandler_Handle_CallsHandlerHandleAsync()
    {
        // Arrange
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequest>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequest>)))
            .Returns(mockRequestHandler.Object);

        var handler = new MediatRRequestHandler<TestAppRequest, MediatRRequest<TestAppRequest>>(
            mockServiceProvider.Object);

        var appRequest = new TestAppRequest { Data = "Test" };
        var request = new MediatRRequest<TestAppRequest>(appRequest);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockRequestHandler.Verify(x => x.HandleAsync(appRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MediatRRequestHandler_Handle_PassesRequestToHandler()
    {
        // Arrange
        TestAppRequest? capturedRequest = null;
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequest>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestAppRequest, CancellationToken>((r, ct) => capturedRequest = r)
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequest>)))
            .Returns(mockRequestHandler.Object);

        var handler = new MediatRRequestHandler<TestAppRequest, MediatRRequest<TestAppRequest>>(
            mockServiceProvider.Object);

        var appRequest = new TestAppRequest { Data = "TestData" };
        var request = new MediatRRequest<TestAppRequest>(appRequest);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Data.Should().Be("TestData");
    }

    [Fact]
    public async Task MediatRRequestHandler_Handle_WithNullHandler_ThrowsNullReferenceException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequest>)))
            .Returns(null!);

        var handler = new MediatRRequestHandler<TestAppRequest, MediatRRequest<TestAppRequest>>(
            mockServiceProvider.Object);

        var request = new MediatRRequest<TestAppRequest>(new TestAppRequest { Data = "Test" });

        // Act
        Func<Task> act = async () => await handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    #endregion

    #region MediatRRequestHandler<T, TRequest, TResponse> Constructor Tests

    [Fact]
    public void MediatRRequestHandlerWithResponse_Constructor_WithValidServiceProvider_CreatesInstance()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatRRequestHandler<TestAppRequestWithResponse, MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void MediatRRequestHandlerWithResponse_ImplementsIRequestHandler()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatRRequestHandler<TestAppRequestWithResponse, MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().BeAssignableTo<IRequestHandler<MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>>();
    }

    #endregion

    #region MediatRRequestHandler<T, TRequest, TResponse> Handle Tests

    [Fact]
    public async Task MediatRRequestHandlerWithResponse_Handle_ResolvesHandlerFromServiceProvider()
    {
        // Arrange
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequestWithResponse, TestResponse>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequestWithResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResponse { Result = "Success" });

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequestWithResponse, TestResponse>)))
            .Returns(mockRequestHandler.Object);

        var handler = new MediatRRequestHandler<TestAppRequestWithResponse, MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>(
            mockServiceProvider.Object);

        var request = new MediatRRequest<TestAppRequestWithResponse, TestResponse>(
            new TestAppRequestWithResponse { Query = "Test" });

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockServiceProvider.Verify(
            x => x.GetService(typeof(IAppRequestHandler<TestAppRequestWithResponse, TestResponse>)),
            Times.Once);
    }

    [Fact]
    public async Task MediatRRequestHandlerWithResponse_Handle_ReturnsResponseFromHandler()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "ExpectedResult" };
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequestWithResponse, TestResponse>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequestWithResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequestWithResponse, TestResponse>)))
            .Returns(mockRequestHandler.Object);

        var handler = new MediatRRequestHandler<TestAppRequestWithResponse, MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>(
            mockServiceProvider.Object);

        var request = new MediatRRequest<TestAppRequestWithResponse, TestResponse>(
            new TestAppRequestWithResponse { Query = "Test" });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Result.Should().Be("ExpectedResult");
    }

    [Fact]
    public async Task MediatRRequestHandlerWithResponse_Handle_PassesRequestToHandler()
    {
        // Arrange
        TestAppRequestWithResponse? capturedRequest = null;
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequestWithResponse, TestResponse>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequestWithResponse>(), It.IsAny<CancellationToken>()))
            .Callback<TestAppRequestWithResponse, CancellationToken>((r, ct) => capturedRequest = r)
            .ReturnsAsync(new TestResponse());

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequestWithResponse, TestResponse>)))
            .Returns(mockRequestHandler.Object);

        var handler = new MediatRRequestHandler<TestAppRequestWithResponse, MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>(
            mockServiceProvider.Object);

        var appRequest = new TestAppRequestWithResponse { Query = "TestQuery" };
        var request = new MediatRRequest<TestAppRequestWithResponse, TestResponse>(appRequest);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Query.Should().Be("TestQuery");
    }

    [Fact]
    public async Task MediatRRequestHandlerWithResponse_Handle_WithNullHandler_ThrowsNullReferenceException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IAppRequestHandler<TestAppRequestWithResponse, TestResponse>)))
            .Returns(null!);

        var handler = new MediatRRequestHandler<TestAppRequestWithResponse, MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>(
            mockServiceProvider.Object);

        var request = new MediatRRequest<TestAppRequestWithResponse, TestResponse>(
            new TestAppRequestWithResponse { Query = "Test" });

        // Act
        Func<Task> act = async () => await handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task MediatRRequestHandler_Handle_WithRealServiceProvider_ResolvesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequest>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(mockRequestHandler.Object);

        var serviceProvider = services.BuildServiceProvider();

        var handler = new MediatRRequestHandler<TestAppRequest, MediatRRequest<TestAppRequest>>(
            serviceProvider);

        var request = new MediatRRequest<TestAppRequest>(new TestAppRequest { Data = "Integration Test" });

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockRequestHandler.Verify(x => x.HandleAsync(It.IsAny<TestAppRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MediatRRequestHandlerWithResponse_Handle_WithRealServiceProvider_ResolvesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedResponse = new TestResponse { Result = "IntegrationResult" };
        var mockRequestHandler = new Mock<IAppRequestHandler<TestAppRequestWithResponse, TestResponse>>();
        mockRequestHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestAppRequestWithResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        services.AddSingleton(mockRequestHandler.Object);

        var serviceProvider = services.BuildServiceProvider();

        var handler = new MediatRRequestHandler<TestAppRequestWithResponse, MediatRRequest<TestAppRequestWithResponse, TestResponse>, TestResponse>(
            serviceProvider);

        var request = new MediatRRequest<TestAppRequestWithResponse, TestResponse>(
            new TestAppRequestWithResponse { Query = "Integration Test" });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    #endregion

    #region Test Helper Classes

    public class TestAppRequest : IAppRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestAppRequestWithResponse : IAppRequest<TestResponse>
    {
        public string Query { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    #endregion
}
