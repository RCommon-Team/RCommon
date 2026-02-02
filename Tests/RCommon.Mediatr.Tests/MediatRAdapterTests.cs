using FluentAssertions;
using MediatR;
using Moq;
using RCommon.MediatR;
using RCommon.MediatR.Subscribers;
using Xunit;

namespace RCommon.Mediatr.Tests;

public class MediatRAdapterTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidMediator_CreatesInstance()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();

        // Act
        var adapter = new MediatRAdapter(mockMediator.Object);

        // Assert
        adapter.Should().NotBeNull();
    }

    [Fact]
    public void MediatRAdapter_ImplementsIMediatorAdapter()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();

        // Act
        var adapter = new MediatRAdapter(mockMediator.Object);

        // Assert
        adapter.Should().BeAssignableTo<RCommon.Mediator.IMediatorAdapter>();
    }

    #endregion

    #region Publish Tests

    [Fact]
    public async Task Publish_DelegatesToMediatorPublish()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var adapter = new MediatRAdapter(mockMediator.Object);
        var notification = new TestNotification { Message = "Test" };

        // Act
        await adapter.Publish(notification);

        // Assert
        mockMediator.Verify(
            x => x.Publish(It.IsAny<MediatRNotification<TestNotification>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Publish_WrapsNotificationInMediatRNotification()
    {
        // Arrange
        MediatRNotification<TestNotification>? capturedNotification = null;
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Publish(It.IsAny<MediatRNotification<TestNotification>>(), It.IsAny<CancellationToken>()))
            .Callback<MediatRNotification<TestNotification>, CancellationToken>((n, ct) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        var adapter = new MediatRAdapter(mockMediator.Object);
        var notification = new TestNotification { Message = "Hello World" };

        // Act
        await adapter.Publish(notification);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification.Should().BeOfType<MediatRNotification<TestNotification>>();
        capturedNotification!.Notification.Should().BeSameAs(notification);
        capturedNotification.Notification.Message.Should().Be("Hello World");
    }

    [Fact]
    public async Task Publish_PassesCancellationTokenToMediator()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Publish(It.IsAny<MediatRNotification<TestNotification>>(), It.IsAny<CancellationToken>()))
            .Callback<MediatRNotification<TestNotification>, CancellationToken>((n, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var adapter = new MediatRAdapter(mockMediator.Object);
        var notification = new TestNotification();
        using var cts = new CancellationTokenSource();

        // Act
        await adapter.Publish(notification, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Publish_WithDefaultCancellationToken_PassesDefaultToMediator()
    {
        // Arrange
        CancellationToken capturedToken = new CancellationToken(true);
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Publish(It.IsAny<MediatRNotification<TestNotification>>(), It.IsAny<CancellationToken>()))
            .Callback<MediatRNotification<TestNotification>, CancellationToken>((n, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var adapter = new MediatRAdapter(mockMediator.Object);
        var notification = new TestNotification();

        // Act
        await adapter.Publish(notification);

        // Assert
        capturedToken.Should().Be(default(CancellationToken));
    }

    #endregion

    #region Send (void) Tests

    [Fact]
    public async Task Send_DelegatesToMediatorSend()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var adapter = new MediatRAdapter(mockMediator.Object);
        var request = new TestRequest { Data = "Test" };

        // Act
        await adapter.Send(request);

        // Assert
        mockMediator.Verify(
            x => x.Send(It.IsAny<MediatRRequest<TestRequest>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Send_WrapsRequestInMediatRRequest()
    {
        // Arrange
        MediatRRequest<TestRequest>? capturedRequest = null;
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send(It.IsAny<MediatRRequest<TestRequest>>(), It.IsAny<CancellationToken>()))
            .Callback<MediatRRequest<TestRequest>, CancellationToken>((r, ct) => capturedRequest = r)
            .Returns(Task.FromResult(default(TestRequest)!));

        var adapter = new MediatRAdapter(mockMediator.Object);
        var request = new TestRequest { Data = "TestData" };

        // Act
        await adapter.Send(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest.Should().BeOfType<MediatRRequest<TestRequest>>();
        capturedRequest!.Request.Should().BeSameAs(request);
        capturedRequest.Request.Data.Should().Be("TestData");
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToMediator()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send(It.IsAny<MediatRRequest<TestRequest>>(), It.IsAny<CancellationToken>()))
            .Callback<MediatRRequest<TestRequest>, CancellationToken>((r, ct) => capturedToken = ct)
            .Returns(Task.FromResult(default(TestRequest)!));

        var adapter = new MediatRAdapter(mockMediator.Object);
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();

        // Act
        await adapter.Send(request, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    #endregion

    #region Send<TRequest, TResponse> Tests

    [Fact]
    public async Task SendWithResponse_DelegatesToMediatorSend()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "Success" };
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send<TestResponse>(It.IsAny<IRequest<TestResponse>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var adapter = new MediatRAdapter(mockMediator.Object);
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var result = await adapter.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mockMediator.Verify(
            x => x.Send<TestResponse>(It.IsAny<IRequest<TestResponse>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendWithResponse_ReturnsResponseFromMediator()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "ExpectedResult" };
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send<TestResponse>(It.IsAny<IRequest<TestResponse>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var adapter = new MediatRAdapter(mockMediator.Object);
        var request = new TestRequestWithResponse();

        // Act
        var result = await adapter.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Result.Should().Be("ExpectedResult");
    }

    [Fact]
    public async Task SendWithResponse_WrapsRequestInMediatRRequest()
    {
        // Arrange
        IRequest<TestResponse>? capturedRequest = null;
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send<TestResponse>(It.IsAny<IRequest<TestResponse>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TestResponse>, CancellationToken>((r, ct) => capturedRequest = r)
            .ReturnsAsync(new TestResponse());

        var adapter = new MediatRAdapter(mockMediator.Object);
        var request = new TestRequestWithResponse { Query = "TestQuery" };

        // Act
        await adapter.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest.Should().BeOfType<MediatRRequest<TestRequestWithResponse, TestResponse>>();
        var mediatRRequest = (MediatRRequest<TestRequestWithResponse, TestResponse>)capturedRequest!;
        mediatRRequest.Request.Should().BeSameAs(request);
        mediatRRequest.Request.Query.Should().Be("TestQuery");
    }

    [Fact]
    public async Task SendWithResponse_PassesCancellationTokenToMediator()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send<TestResponse>(It.IsAny<IRequest<TestResponse>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TestResponse>, CancellationToken>((r, ct) => capturedToken = ct)
            .ReturnsAsync(new TestResponse());

        var adapter = new MediatRAdapter(mockMediator.Object);
        var request = new TestRequestWithResponse();
        using var cts = new CancellationTokenSource();

        // Act
        await adapter.Send<TestRequestWithResponse, TestResponse>(request, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public async Task MultiplePublishCalls_EachCallDelegatesToMediator()
    {
        // Arrange
        var callCount = 0;
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Publish(It.IsAny<MediatRNotification<TestNotification>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .Returns(Task.CompletedTask);

        var adapter = new MediatRAdapter(mockMediator.Object);

        // Act
        await adapter.Publish(new TestNotification());
        await adapter.Publish(new TestNotification());
        await adapter.Publish(new TestNotification());

        // Assert
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task MixedOperations_AllDelegateCorrectly()
    {
        // Arrange
        var publishCount = 0;
        var sendVoidCount = 0;
        var sendWithResponseCount = 0;

        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Publish(It.IsAny<MediatRNotification<TestNotification>>(), It.IsAny<CancellationToken>()))
            .Callback(() => publishCount++)
            .Returns(Task.CompletedTask);
        mockMediator
            .Setup(x => x.Send(It.IsAny<MediatRRequest<TestRequest>>(), It.IsAny<CancellationToken>()))
            .Callback(() => sendVoidCount++)
            .Returns(Task.FromResult(default(TestRequest)!));
        mockMediator
            .Setup(x => x.Send<TestResponse>(It.IsAny<IRequest<TestResponse>>(), It.IsAny<CancellationToken>()))
            .Callback(() => sendWithResponseCount++)
            .ReturnsAsync(new TestResponse());

        var adapter = new MediatRAdapter(mockMediator.Object);

        // Act
        await adapter.Publish(new TestNotification());
        await adapter.Send(new TestRequest());
        await adapter.Send<TestRequestWithResponse, TestResponse>(new TestRequestWithResponse());

        // Assert
        publishCount.Should().Be(1);
        sendVoidCount.Should().Be(1);
        sendWithResponseCount.Should().Be(1);
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

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    #endregion
}
