using FluentAssertions;
using Moq;
using RCommon.Mediator;
using Xunit;

namespace RCommon.Mediator.Tests;

public class IMediatorServiceTests
{
    #region Interface Implementation Tests

    [Fact]
    public void IMediatorService_CanBeImplemented()
    {
        // Arrange & Act
        var service = new TestMediatorService();

        // Assert
        service.Should().BeAssignableTo<IMediatorService>();
    }

    [Fact]
    public void IMediatorService_CanBeMocked()
    {
        // Arrange & Act
        var mockService = new Mock<IMediatorService>();

        // Assert
        mockService.Object.Should().BeAssignableTo<IMediatorService>();
    }

    [Fact]
    public void MediatorService_ImplementsIMediatorService()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();

        // Act
        var service = new MediatorService(mockAdapter.Object);

        // Assert
        service.Should().BeAssignableTo<IMediatorService>();
    }

    #endregion

    #region Publish Method Tests

    [Fact]
    public async Task Publish_CanBeInvoked()
    {
        // Arrange
        var service = new TestMediatorService();
        var notification = new TestNotification { Message = "Test" };

        // Act
        var act = async () => await service.Publish(notification);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_MockedService_CanVerifyInvocation()
    {
        // Arrange
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notification = new TestNotification { Message = "Test" };

        // Act
        await mockService.Object.Publish(notification);

        // Assert
        mockService.Verify(x => x.Publish(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_MockedService_CanCaptureNotification()
    {
        // Arrange
        TestNotification? capturedNotification = null;
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestNotification, CancellationToken>((n, ct) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        var notification = new TestNotification { Message = "CapturedMessage" };

        // Act
        await mockService.Object.Publish(notification);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Message.Should().Be("CapturedMessage");
    }

    [Fact]
    public async Task Publish_SupportsCancellationToken()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestNotification, CancellationToken>((n, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();

        // Act
        await mockService.Object.Publish(new TestNotification(), cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Publish_SupportsDefaultCancellationToken()
    {
        // Arrange
        CancellationToken capturedToken = new CancellationToken(true);
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestNotification, CancellationToken>((n, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        // Act
        await mockService.Object.Publish(new TestNotification());

        // Assert
        capturedToken.Should().Be(default(CancellationToken));
    }

    #endregion

    #region Send<TRequest> Method Tests

    [Fact]
    public async Task Send_CanBeInvoked()
    {
        // Arrange
        var service = new TestMediatorService();
        var request = new TestRequest { Data = "Test" };

        // Act
        var act = async () => await service.Send(request);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Send_MockedService_CanVerifyInvocation()
    {
        // Arrange
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new TestRequest { Data = "Test" };

        // Act
        await mockService.Object.Send(request);

        // Assert
        mockService.Verify(x => x.Send(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_MockedService_CanCaptureRequest()
    {
        // Arrange
        TestRequest? capturedRequest = null;
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, CancellationToken>((r, ct) => capturedRequest = r)
            .Returns(Task.CompletedTask);

        var request = new TestRequest { Data = "CapturedData" };

        // Act
        await mockService.Object.Send(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Data.Should().Be("CapturedData");
    }

    [Fact]
    public async Task Send_SupportsCancellationToken()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, CancellationToken>((r, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();

        // Act
        await mockService.Object.Send(new TestRequest(), cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    #endregion

    #region Send<TRequest, TResponse> Method Tests

    [Fact]
    public async Task SendWithResponse_CanBeInvoked()
    {
        // Arrange
        var service = new TestMediatorService();
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var result = await service.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SendWithResponse_MockedService_ReturnsConfiguredResponse()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "ExpectedResult" };
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new TestRequestWithResponse();

        // Act
        var result = await mockService.Object.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Result.Should().Be("ExpectedResult");
    }

    [Fact]
    public async Task SendWithResponse_MockedService_CanVerifyInvocation()
    {
        // Arrange
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResponse());

        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        await mockService.Object.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mockService.Verify(
            x => x.Send<TestRequestWithResponse, TestResponse>(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendWithResponse_MockedService_CanCaptureRequest()
    {
        // Arrange
        TestRequestWithResponse? capturedRequest = null;
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequestWithResponse, CancellationToken>((r, ct) => capturedRequest = r)
            .ReturnsAsync(new TestResponse());

        var request = new TestRequestWithResponse { Query = "CapturedQuery" };

        // Act
        await mockService.Object.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Query.Should().Be("CapturedQuery");
    }

    [Fact]
    public async Task SendWithResponse_SupportsCancellationToken()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequestWithResponse, CancellationToken>((r, ct) => capturedToken = ct)
            .ReturnsAsync(new TestResponse());

        using var cts = new CancellationTokenSource();

        // Act
        await mockService.Object.Send<TestRequestWithResponse, TestResponse>(new TestRequestWithResponse(), cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SendWithResponse_SupportsValueTypeResponse()
    {
        // Arrange
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send<IntRequest, int>(It.IsAny<IntRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await mockService.Object.Send<IntRequest, int>(new IntRequest());

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task SendWithResponse_SupportsNullableResponse()
    {
        // Arrange
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse?>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse?)null);

        // Act
        var result = await mockService.Object.Send<TestRequestWithResponse, TestResponse?>(new TestRequestWithResponse());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public async Task IMediatorService_SupportsMultipleOperations()
    {
        // Arrange
        var publishCount = 0;
        var sendCount = 0;
        var sendWithResponseCount = 0;

        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback(() => publishCount++)
            .Returns(Task.CompletedTask);
        mockService
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => sendCount++)
            .Returns(Task.CompletedTask);
        mockService
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendWithResponseCount++)
            .ReturnsAsync(new TestResponse());

        // Act
        await mockService.Object.Publish(new TestNotification());
        await mockService.Object.Send(new TestRequest());
        await mockService.Object.Send<TestRequestWithResponse, TestResponse>(new TestRequestWithResponse());

        // Assert
        publishCount.Should().Be(1);
        sendCount.Should().Be(1);
        sendWithResponseCount.Should().Be(1);
    }

    [Fact]
    public async Task IMediatorService_CanBeDependencyInjected()
    {
        // Arrange
        var mockService = new Mock<IMediatorService>();
        mockService
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResponse { Result = "DIResult" });

        var consumer = new MediatorServiceConsumer(mockService.Object);

        // Act
        var result = await consumer.ProcessRequest(new TestRequestWithResponse());

        // Assert
        result.Result.Should().Be("DIResult");
    }

    #endregion

    #region Test Helper Classes

    public class TestNotification
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestRequestWithResponse
    {
        public string Query { get; set; } = string.Empty;
    }

    public class IntRequest
    {
        public int Value { get; set; }
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    public class TestMediatorService : IMediatorService
    {
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Activator.CreateInstance<TResponse>());
        }
    }

    public class MediatorServiceConsumer
    {
        private readonly IMediatorService _mediatorService;

        public MediatorServiceConsumer(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public Task<TestResponse> ProcessRequest(TestRequestWithResponse request)
        {
            return _mediatorService.Send<TestRequestWithResponse, TestResponse>(request);
        }
    }

    #endregion
}
