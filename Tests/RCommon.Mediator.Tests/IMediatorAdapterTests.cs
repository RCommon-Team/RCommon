using FluentAssertions;
using Moq;
using RCommon.Mediator;
using Xunit;

namespace RCommon.Mediator.Tests;

public class IMediatorAdapterTests
{
    #region Interface Implementation Tests

    [Fact]
    public void IMediatorAdapter_CanBeImplemented()
    {
        // Arrange & Act
        var adapter = new TestMediatorAdapter();

        // Assert
        adapter.Should().BeAssignableTo<IMediatorAdapter>();
    }

    [Fact]
    public void IMediatorAdapter_CanBeMocked()
    {
        // Arrange & Act
        var mockAdapter = new Mock<IMediatorAdapter>();

        // Assert
        mockAdapter.Object.Should().BeAssignableTo<IMediatorAdapter>();
    }

    #endregion

    #region Send<TRequest> Tests

    [Fact]
    public async Task Send_CanBeInvoked()
    {
        // Arrange
        var adapter = new TestMediatorAdapter();
        var request = new TestRequest { Data = "Test" };

        // Act
        var act = async () => await adapter.Send(request);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Send_ReceivesRequest()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var request = new TestRequest { Data = "TestData" };

        // Act
        await adapter.Send(request);

        // Assert
        adapter.LastSentRequest.Should().NotBeNull();
        adapter.LastSentRequest.Should().Be(request);
    }

    [Fact]
    public async Task Send_ReceivesCancellationToken()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();

        // Act
        await adapter.Send(request, cts.Token);

        // Assert
        adapter.LastCancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Send_UsesDefaultCancellationToken_WhenNotProvided()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var request = new TestRequest();

        // Act
        await adapter.Send(request);

        // Assert
        adapter.LastCancellationToken.Should().Be(default(CancellationToken));
    }

    [Fact]
    public async Task Send_MockedAdapter_CanVerifyInvocation()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new TestRequest { Data = "Test" };

        // Act
        await mockAdapter.Object.Send(request);

        // Assert
        mockAdapter.Verify(x => x.Send(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_MockedAdapter_CanCaptureRequestData()
    {
        // Arrange
        TestRequest? capturedRequest = null;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, CancellationToken>((r, ct) => capturedRequest = r)
            .Returns(Task.CompletedTask);

        var request = new TestRequest { Data = "CapturedData" };

        // Act
        await mockAdapter.Object.Send(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Data.Should().Be("CapturedData");
    }

    #endregion

    #region Send<TRequest, TResponse> Tests

    [Fact]
    public async Task SendWithResponse_CanBeInvoked()
    {
        // Arrange
        var adapter = new TestMediatorAdapter();
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var result = await adapter.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SendWithResponse_ReturnsResponse()
    {
        // Arrange
        var adapter = new TestMediatorAdapter();
        var request = new TestRequestWithResponse { Query = "TestQuery" };

        // Act
        var result = await adapter.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().Be("Response for: TestQuery");
    }

    [Fact]
    public async Task SendWithResponse_ReceivesRequest()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var request = new TestRequestWithResponse { Query = "QueryData" };

        // Act
        await adapter.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        adapter.LastSentRequestWithResponse.Should().NotBeNull();
        adapter.LastSentRequestWithResponse.Should().Be(request);
    }

    [Fact]
    public async Task SendWithResponse_ReceivesCancellationToken()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var request = new TestRequestWithResponse();
        using var cts = new CancellationTokenSource();

        // Act
        await adapter.Send<TestRequestWithResponse, TestResponse>(request, cts.Token);

        // Assert
        adapter.LastCancellationTokenWithResponse.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SendWithResponse_UsesDefaultCancellationToken_WhenNotProvided()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var request = new TestRequestWithResponse();

        // Act
        await adapter.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        adapter.LastCancellationTokenWithResponse.Should().Be(default(CancellationToken));
    }

    [Fact]
    public async Task SendWithResponse_MockedAdapter_ReturnsConfiguredResponse()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "MockedResult" };
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new TestRequestWithResponse();

        // Act
        var result = await mockAdapter.Object.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Result.Should().Be("MockedResult");
    }

    [Fact]
    public async Task SendWithResponse_SupportsValueTypeResponse()
    {
        // Arrange
        var adapter = new ValueTypeResponseAdapter();
        var request = new IntRequest { Value = 10 };

        // Act
        var result = await adapter.Send<IntRequest, int>(request);

        // Assert
        result.Should().Be(20);
    }

    #endregion

    #region Publish Tests

    [Fact]
    public async Task Publish_CanBeInvoked()
    {
        // Arrange
        var adapter = new TestMediatorAdapter();
        var notification = new TestNotification { Message = "Test" };

        // Act
        var act = async () => await adapter.Publish(notification);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_ReceivesNotification()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var notification = new TestNotification { Message = "TestMessage" };

        // Act
        await adapter.Publish(notification);

        // Assert
        adapter.LastPublishedNotification.Should().NotBeNull();
        adapter.LastPublishedNotification.Should().Be(notification);
    }

    [Fact]
    public async Task Publish_ReceivesCancellationToken()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var notification = new TestNotification();
        using var cts = new CancellationTokenSource();

        // Act
        await adapter.Publish(notification, cts.Token);

        // Assert
        adapter.LastPublishCancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Publish_UsesDefaultCancellationToken_WhenNotProvided()
    {
        // Arrange
        var adapter = new CapturingMediatorAdapter();
        var notification = new TestNotification();

        // Act
        await adapter.Publish(notification);

        // Assert
        adapter.LastPublishCancellationToken.Should().Be(default(CancellationToken));
    }

    [Fact]
    public async Task Publish_MockedAdapter_CanVerifyInvocation()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notification = new TestNotification { Message = "Test" };

        // Act
        await mockAdapter.Object.Publish(notification);

        // Assert
        mockAdapter.Verify(x => x.Publish(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_MockedAdapter_CanCaptureNotificationData()
    {
        // Arrange
        TestNotification? capturedNotification = null;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestNotification, CancellationToken>((n, ct) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        var notification = new TestNotification { Message = "CapturedMessage" };

        // Act
        await mockAdapter.Object.Publish(notification);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Message.Should().Be("CapturedMessage");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public async Task IMediatorAdapter_SupportsMultipleDifferentRequestTypes()
    {
        // Arrange
        var adapter = new MultiTypeAdapter();

        // Act
        await adapter.Send(new TestRequest { Data = "Request1" });
        await adapter.Send(new AnotherRequest { Code = "Request2" });

        // Assert
        adapter.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task IMediatorAdapter_SupportsMultipleDifferentNotificationTypes()
    {
        // Arrange
        var adapter = new MultiTypeAdapter();

        // Act
        await adapter.Publish(new TestNotification { Message = "Notification1" });
        await adapter.Publish(new AnotherNotification { Code = "Notification2" });

        // Assert
        adapter.NotificationCount.Should().Be(2);
    }

    [Fact]
    public async Task IMediatorAdapter_HandlesCancellationGracefully()
    {
        // Arrange
        var adapter = new CancellationRespectingAdapter();
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await adapter.Send(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Test Helper Classes

    public class TestRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class AnotherRequest
    {
        public string Code { get; set; } = string.Empty;
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

    public class TestNotification
    {
        public string Message { get; set; } = string.Empty;
    }

    public class AnotherNotification
    {
        public string Code { get; set; } = string.Empty;
    }

    public class TestMediatorAdapter : IMediatorAdapter
    {
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            if (request is TestRequestWithResponse reqWithResponse && typeof(TResponse) == typeof(TestResponse))
            {
                return Task.FromResult((TResponse)(object)new TestResponse { Result = $"Response for: {reqWithResponse.Query}" });
            }
            return Task.FromResult(default(TResponse)!);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class CapturingMediatorAdapter : IMediatorAdapter
    {
        public object? LastSentRequest { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public object? LastSentRequestWithResponse { get; private set; }
        public CancellationToken LastCancellationTokenWithResponse { get; private set; }
        public object? LastPublishedNotification { get; private set; }
        public CancellationToken LastPublishCancellationToken { get; private set; }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            LastSentRequest = request;
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            LastSentRequestWithResponse = request;
            LastCancellationTokenWithResponse = cancellationToken;
            return Task.FromResult(default(TResponse)!);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            LastPublishedNotification = notification;
            LastPublishCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    public class ValueTypeResponseAdapter : IMediatorAdapter
    {
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            if (request is IntRequest intRequest && typeof(TResponse) == typeof(int))
            {
                return Task.FromResult((TResponse)(object)(intRequest.Value * 2));
            }
            return Task.FromResult(default(TResponse)!);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class MultiTypeAdapter : IMediatorAdapter
    {
        public int RequestCount { get; private set; }
        public int NotificationCount { get; private set; }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return Task.FromResult(default(TResponse)!);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            NotificationCount++;
            return Task.CompletedTask;
        }
    }

    public class CancellationRespectingAdapter : IMediatorAdapter
    {
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(default(TResponse)!);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    #endregion
}
