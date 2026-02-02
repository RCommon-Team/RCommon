using FluentAssertions;
using Moq;
using RCommon.Mediator;
using Xunit;

namespace RCommon.Mediator.Tests;

public class MediatorServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidMediatorAdapter_CreatesInstance()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();

        // Act
        var service = new MediatorService(mockAdapter.Object);

        // Assert
        service.Should().NotBeNull();
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

    #region Publish Tests

    [Fact]
    public async Task Publish_DelegatesToMediatorAdapter()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var notification = new TestNotification { Message = "Test" };

        // Act
        await service.Publish(notification);

        // Assert
        mockAdapter.Verify(x => x.Publish(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_PassesNotificationToAdapter()
    {
        // Arrange
        TestNotification? capturedNotification = null;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestNotification, CancellationToken>((n, ct) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var notification = new TestNotification { Message = "Hello World" };

        // Act
        await service.Publish(notification);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Message.Should().Be("Hello World");
    }

    [Fact]
    public async Task Publish_PassesCancellationTokenToAdapter()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestNotification, CancellationToken>((n, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var notification = new TestNotification();
        using var cts = new CancellationTokenSource();

        // Act
        await service.Publish(notification, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Publish_WithDefaultCancellationToken_PassesDefaultToAdapter()
    {
        // Arrange
        CancellationToken capturedToken = new CancellationToken(true); // Non-default value
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestNotification, CancellationToken>((n, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var notification = new TestNotification();

        // Act
        await service.Publish(notification);

        // Assert
        capturedToken.Should().Be(default(CancellationToken));
    }

    [Fact]
    public async Task Publish_WithComplexNotification_PassesCorrectly()
    {
        // Arrange
        ComplexNotification? capturedNotification = null;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<ComplexNotification>(), It.IsAny<CancellationToken>()))
            .Callback<ComplexNotification, CancellationToken>((n, ct) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var notification = new ComplexNotification
        {
            Id = 42,
            Name = "ComplexTest",
            Items = new List<string> { "Item1", "Item2" }
        };

        // Act
        await service.Publish(notification);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Id.Should().Be(42);
        capturedNotification.Name.Should().Be("ComplexTest");
        capturedNotification.Items.Should().BeEquivalentTo(new[] { "Item1", "Item2" });
    }

    #endregion

    #region Send (void) Tests

    [Fact]
    public async Task Send_DelegatesToMediatorAdapter()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequest { Data = "Test" };

        // Act
        await service.Send(request);

        // Assert
        mockAdapter.Verify(x => x.Send(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_PassesRequestToAdapter()
    {
        // Arrange
        TestRequest? capturedRequest = null;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, CancellationToken>((r, ct) => capturedRequest = r)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequest { Data = "TestData" };

        // Act
        await service.Send(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Data.Should().Be("TestData");
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToAdapter()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, CancellationToken>((r, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();

        // Act
        await service.Send(request, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Send_WithDefaultCancellationToken_PassesDefaultToAdapter()
    {
        // Arrange
        CancellationToken capturedToken = new CancellationToken(true);
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, CancellationToken>((r, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequest();

        // Act
        await service.Send(request);

        // Assert
        capturedToken.Should().Be(default(CancellationToken));
    }

    #endregion

    #region Send<TRequest, TResponse> Tests

    [Fact]
    public async Task Send_WithResponse_DelegatesToMediatorAdapter()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "Success" };
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var result = await service.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mockAdapter.Verify(
            x => x.Send<TestRequestWithResponse, TestResponse>(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Send_WithResponse_ReturnsResponseFromAdapter()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "ExpectedResult" };
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequestWithResponse();

        // Act
        var result = await service.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Result.Should().Be("ExpectedResult");
    }

    [Fact]
    public async Task Send_WithResponse_PassesRequestToAdapter()
    {
        // Arrange
        TestRequestWithResponse? capturedRequest = null;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequestWithResponse, CancellationToken>((r, ct) => capturedRequest = r)
            .ReturnsAsync(new TestResponse());

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequestWithResponse { Query = "TestQuery" };

        // Act
        await service.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Query.Should().Be("TestQuery");
    }

    [Fact]
    public async Task Send_WithResponse_PassesCancellationTokenToAdapter()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequestWithResponse, CancellationToken>((r, ct) => capturedToken = ct)
            .ReturnsAsync(new TestResponse());

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequestWithResponse();
        using var cts = new CancellationTokenSource();

        // Act
        await service.Send<TestRequestWithResponse, TestResponse>(request, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Send_WithResponse_WithDefaultCancellationToken_PassesDefaultToAdapter()
    {
        // Arrange
        CancellationToken capturedToken = new CancellationToken(true);
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequestWithResponse, CancellationToken>((r, ct) => capturedToken = ct)
            .ReturnsAsync(new TestResponse());

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequestWithResponse();

        // Act
        await service.Send<TestRequestWithResponse, TestResponse>(request);

        // Assert
        capturedToken.Should().Be(default(CancellationToken));
    }

    [Fact]
    public async Task Send_WithValueTypeResponse_ReturnsCorrectValue()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithIntResponse, int>(
                It.IsAny<TestRequestWithIntResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequestWithIntResponse();

        // Act
        var result = await service.Send<TestRequestWithIntResponse, int>(request);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task Send_WithNullableResponse_ReturnsNull()
    {
        // Arrange
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse?>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse?)null);

        var service = new MediatorService(mockAdapter.Object);
        var request = new TestRequestWithResponse();

        // Act
        var result = await service.Send<TestRequestWithResponse, TestResponse?>(request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public async Task MultiplePublishCalls_EachCallDelegatesToAdapter()
    {
        // Arrange
        var callCount = 0;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);

        // Act
        await service.Publish(new TestNotification());
        await service.Publish(new TestNotification());
        await service.Publish(new TestNotification());

        // Assert
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task MultipleSendCalls_EachCallDelegatesToAdapter()
    {
        // Arrange
        var callCount = 0;
        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .Returns(Task.CompletedTask);

        var service = new MediatorService(mockAdapter.Object);

        // Act
        await service.Send(new TestRequest());
        await service.Send(new TestRequest());

        // Assert
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task MixedOperations_AllDelegateCorrectly()
    {
        // Arrange
        var publishCount = 0;
        var sendVoidCount = 0;
        var sendWithResponseCount = 0;

        var mockAdapter = new Mock<IMediatorAdapter>();
        mockAdapter
            .Setup(x => x.Publish(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Callback(() => publishCount++)
            .Returns(Task.CompletedTask);
        mockAdapter
            .Setup(x => x.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => sendVoidCount++)
            .Returns(Task.CompletedTask);
        mockAdapter
            .Setup(x => x.Send<TestRequestWithResponse, TestResponse>(
                It.IsAny<TestRequestWithResponse>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendWithResponseCount++)
            .ReturnsAsync(new TestResponse());

        var service = new MediatorService(mockAdapter.Object);

        // Act
        await service.Publish(new TestNotification());
        await service.Send(new TestRequest());
        await service.Send<TestRequestWithResponse, TestResponse>(new TestRequestWithResponse());

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

    public class ComplexNotification
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }

    public class TestRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestRequestWithResponse
    {
        public string Query { get; set; } = string.Empty;
    }

    public class TestRequestWithIntResponse
    {
        public int Value { get; set; }
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    #endregion
}
