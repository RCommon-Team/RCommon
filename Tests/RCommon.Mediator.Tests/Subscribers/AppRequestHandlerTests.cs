using FluentAssertions;
using Moq;
using RCommon.Mediator.Subscribers;
using Xunit;

namespace RCommon.Mediator.Tests.Subscribers;

public class AppRequestHandlerTests
{
    #region IAppRequestHandler<TRequest> Interface Tests

    [Fact]
    public void IAppRequestHandler_CanBeImplemented()
    {
        // Arrange & Act
        var handler = new TestAppRequestHandler();

        // Assert
        handler.Should().BeAssignableTo<IAppRequestHandler<TestRequest>>();
    }

    [Fact]
    public async Task IAppRequestHandler_HandleAsync_CanBeInvoked()
    {
        // Arrange
        var handler = new TestAppRequestHandler();
        var request = new TestRequest { Data = "Test" };

        // Act
        var act = async () => await handler.HandleAsync(request);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IAppRequestHandler_HandleAsync_ReceivesRequest()
    {
        // Arrange
        var handler = new CapturingRequestHandler();
        var request = new TestRequest { Data = "TestData" };

        // Act
        await handler.HandleAsync(request);

        // Assert
        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.Data.Should().Be("TestData");
    }

    [Fact]
    public async Task IAppRequestHandler_HandleAsync_ReceivesCancellationToken()
    {
        // Arrange
        var handler = new CancellationAwareRequestHandler();
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();

        // Act
        await handler.HandleAsync(request, cts.Token);

        // Assert
        handler.ReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task IAppRequestHandler_HandleAsync_UseDefaultCancellationToken()
    {
        // Arrange
        var handler = new CancellationAwareRequestHandler();
        var request = new TestRequest();

        // Act
        await handler.HandleAsync(request);

        // Assert
        handler.ReceivedToken.Should().Be(default(CancellationToken));
    }

    [Fact]
    public async Task IAppRequestHandler_HandleAsync_RespondsToCanellation()
    {
        // Arrange
        var handler = new CancellationResponsiveRequestHandler();
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await handler.HandleAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task IAppRequestHandler_HandleAsync_CanPerformAsyncOperations()
    {
        // Arrange
        var handler = new AsyncOperationRequestHandler();
        var request = new TestRequest();

        // Act
        await handler.HandleAsync(request);

        // Assert
        handler.OperationCompleted.Should().BeTrue();
    }

    [Fact]
    public void IAppRequestHandler_CanBeMocked()
    {
        // Arrange
        var mockHandler = new Mock<IAppRequestHandler<TestRequest>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var handler = mockHandler.Object;

        // Assert
        handler.Should().BeAssignableTo<IAppRequestHandler<TestRequest>>();
    }

    [Fact]
    public async Task IAppRequestHandler_MockedHandler_CanVerifyInvocation()
    {
        // Arrange
        var mockHandler = new Mock<IAppRequestHandler<TestRequest>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new TestRequest { Data = "Test" };

        // Act
        await mockHandler.Object.HandleAsync(request);

        // Assert
        mockHandler.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region IAppRequestHandler<TRequest, TResponse> Interface Tests

    [Fact]
    public void IAppRequestHandlerWithResponse_CanBeImplemented()
    {
        // Arrange & Act
        var handler = new TestAppRequestHandlerWithResponse();

        // Assert
        handler.Should().BeAssignableTo<IAppRequestHandler<TestRequestWithResponse, TestResponse>>();
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_ReturnsResponse()
    {
        // Arrange
        var handler = new TestAppRequestHandlerWithResponse();
        var request = new TestRequestWithResponse { Query = "GetData" };

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Processed: GetData");
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_ReceivesRequest()
    {
        // Arrange
        var handler = new CapturingRequestHandlerWithResponse();
        var request = new TestRequestWithResponse { Query = "TestQuery" };

        // Act
        await handler.HandleAsync(request);

        // Assert
        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.Query.Should().Be("TestQuery");
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_ReceivesCancellationToken()
    {
        // Arrange
        var handler = new CancellationAwareRequestHandlerWithResponse();
        var request = new TestRequestWithResponse();
        using var cts = new CancellationTokenSource();

        // Act
        await handler.HandleAsync(request, cts.Token);

        // Assert
        handler.ReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_UseDefaultCancellationToken()
    {
        // Arrange
        var handler = new CancellationAwareRequestHandlerWithResponse();
        var request = new TestRequestWithResponse();

        // Act
        await handler.HandleAsync(request);

        // Assert
        handler.ReceivedToken.Should().Be(default(CancellationToken));
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_RespondsToCanellation()
    {
        // Arrange
        var handler = new CancellationResponsiveRequestHandlerWithResponse();
        var request = new TestRequestWithResponse();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await handler.HandleAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_ReturnsValueType()
    {
        // Arrange
        var handler = new IntResponseHandler();
        var request = new TestRequestForInt { Value = 10 };

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().Be(20);
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_ReturnsCollectionType()
    {
        // Arrange
        var handler = new CollectionResponseHandler();
        var request = new TestRequestForCollection { Count = 3 };

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_HandleAsync_ReturnsNullableType()
    {
        // Arrange
        var handler = new NullableResponseHandler();
        var request = new TestRequestForNullable { ReturnNull = true };

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IAppRequestHandlerWithResponse_CanBeMocked()
    {
        // Arrange
        var mockHandler = new Mock<IAppRequestHandler<TestRequestWithResponse, TestResponse>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestRequestWithResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResponse { Result = "MockedResult" });

        // Act
        var handler = mockHandler.Object;

        // Assert
        handler.Should().BeAssignableTo<IAppRequestHandler<TestRequestWithResponse, TestResponse>>();
    }

    [Fact]
    public async Task IAppRequestHandlerWithResponse_MockedHandler_ReturnsConfiguredResponse()
    {
        // Arrange
        var expectedResponse = new TestResponse { Result = "ExpectedResult" };
        var mockHandler = new Mock<IAppRequestHandler<TestRequestWithResponse, TestResponse>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestRequestWithResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await mockHandler.Object.HandleAsync(new TestRequestWithResponse());

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Result.Should().Be("ExpectedResult");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void IAppRequestHandler_MultipleHandlerImplementations()
    {
        // Arrange & Act
        var handler = new MultiRequestHandler();

        // Assert
        handler.Should().BeAssignableTo<IAppRequestHandler<TestRequest>>();
        handler.Should().BeAssignableTo<IAppRequestHandler<AnotherTestRequest>>();
    }

    [Fact]
    public async Task IAppRequestHandler_MultipleHandlerImplementations_HandleDifferentRequests()
    {
        // Arrange
        var handler = new MultiRequestHandler();
        var request1 = new TestRequest { Data = "Data1" };
        var request2 = new AnotherTestRequest { Code = "Code2" };

        // Act
        await handler.HandleAsync(request1);
        await handler.HandleAsync(request2);

        // Assert
        handler.HandledRequests.Should().HaveCount(2);
        handler.HandledRequests.Should().ContainKeys("TestRequest", "AnotherTestRequest");
    }

    [Fact]
    public async Task IAppRequestHandler_WithDependencies_CanBeCreated()
    {
        // Arrange
        var dependency = new Mock<ITestDependency>();
        dependency.Setup(x => x.Process(It.IsAny<string>())).Returns("Processed");
        var handler = new HandlerWithDependency(dependency.Object);
        var request = new TestRequest { Data = "Input" };

        // Act
        await handler.HandleAsync(request);

        // Assert
        dependency.Verify(x => x.Process("Input"), Times.Once);
    }

    #endregion

    #region Test Helper Classes

    public class TestRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class AnotherTestRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    public class TestRequestWithResponse
    {
        public string Query { get; set; } = string.Empty;
    }

    public class TestRequestForInt
    {
        public int Value { get; set; }
    }

    public class TestRequestForCollection
    {
        public int Count { get; set; }
    }

    public class TestRequestForNullable
    {
        public bool ReturnNull { get; set; }
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    public class TestAppRequestHandler : IAppRequestHandler<TestRequest>
    {
        public Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class CapturingRequestHandler : IAppRequestHandler<TestRequest>
    {
        public TestRequest? CapturedRequest { get; private set; }

        public Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            CapturedRequest = request;
            return Task.CompletedTask;
        }
    }

    public class CancellationAwareRequestHandler : IAppRequestHandler<TestRequest>
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    public class CancellationResponsiveRequestHandler : IAppRequestHandler<TestRequest>
    {
        public Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public class AsyncOperationRequestHandler : IAppRequestHandler<TestRequest>
    {
        public bool OperationCompleted { get; private set; }

        public async Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            OperationCompleted = true;
        }
    }

    public class TestAppRequestHandlerWithResponse : IAppRequestHandler<TestRequestWithResponse, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestRequestWithResponse request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TestResponse { Result = $"Processed: {request.Query}" });
        }
    }

    public class CapturingRequestHandlerWithResponse : IAppRequestHandler<TestRequestWithResponse, TestResponse>
    {
        public TestRequestWithResponse? CapturedRequest { get; private set; }

        public Task<TestResponse> HandleAsync(TestRequestWithResponse request, CancellationToken cancellationToken = default)
        {
            CapturedRequest = request;
            return Task.FromResult(new TestResponse());
        }
    }

    public class CancellationAwareRequestHandlerWithResponse : IAppRequestHandler<TestRequestWithResponse, TestResponse>
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<TestResponse> HandleAsync(TestRequestWithResponse request, CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(new TestResponse());
        }
    }

    public class CancellationResponsiveRequestHandlerWithResponse : IAppRequestHandler<TestRequestWithResponse, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestRequestWithResponse request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new TestResponse());
        }
    }

    public class IntResponseHandler : IAppRequestHandler<TestRequestForInt, int>
    {
        public Task<int> HandleAsync(TestRequestForInt request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(request.Value * 2);
        }
    }

    public class CollectionResponseHandler : IAppRequestHandler<TestRequestForCollection, List<TestResponse>>
    {
        public Task<List<TestResponse>> HandleAsync(TestRequestForCollection request, CancellationToken cancellationToken = default)
        {
            var result = Enumerable.Range(0, request.Count)
                .Select(i => new TestResponse { Result = $"Item{i}" })
                .ToList();
            return Task.FromResult(result);
        }
    }

    public class NullableResponseHandler : IAppRequestHandler<TestRequestForNullable, TestResponse?>
    {
        public Task<TestResponse?> HandleAsync(TestRequestForNullable request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(request.ReturnNull ? null : new TestResponse());
        }
    }

    public class MultiRequestHandler : IAppRequestHandler<TestRequest>, IAppRequestHandler<AnotherTestRequest>
    {
        public Dictionary<string, object> HandledRequests { get; } = new();

        public Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            HandledRequests["TestRequest"] = request;
            return Task.CompletedTask;
        }

        public Task HandleAsync(AnotherTestRequest request, CancellationToken cancellationToken = default)
        {
            HandledRequests["AnotherTestRequest"] = request;
            return Task.CompletedTask;
        }
    }

    public interface ITestDependency
    {
        string Process(string input);
    }

    public class HandlerWithDependency : IAppRequestHandler<TestRequest>
    {
        private readonly ITestDependency _dependency;

        public HandlerWithDependency(ITestDependency dependency)
        {
            _dependency = dependency;
        }

        public Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            _dependency.Process(request.Data);
            return Task.CompletedTask;
        }
    }

    #endregion
}
