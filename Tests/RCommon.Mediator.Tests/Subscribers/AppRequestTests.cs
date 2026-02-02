using FluentAssertions;
using RCommon.Mediator.Subscribers;
using Xunit;

namespace RCommon.Mediator.Tests.Subscribers;

public class AppRequestTests
{
    #region IAppRequest Interface Tests

    [Fact]
    public void IAppRequest_CanBeImplemented()
    {
        // Arrange & Act
        var request = new TestAppRequest();

        // Assert
        request.Should().BeAssignableTo<IAppRequest>();
    }

    [Fact]
    public void IAppRequest_ImplementationCanContainProperties()
    {
        // Arrange & Act
        var request = new TestAppRequest
        {
            Id = 42,
            Name = "TestRequest",
            Parameters = new Dictionary<string, object> { { "key", "value" } }
        };

        // Assert
        request.Id.Should().Be(42);
        request.Name.Should().Be("TestRequest");
        request.Parameters.Should().ContainKey("key");
    }

    [Fact]
    public void IAppRequest_CanBeUsedAsTypeParameter()
    {
        // Arrange
        var requests = new List<IAppRequest>();

        // Act
        requests.Add(new TestAppRequest());
        requests.Add(new AnotherTestAppRequest());

        // Assert
        requests.Should().HaveCount(2);
        requests.Should().AllBeAssignableTo<IAppRequest>();
    }

    [Fact]
    public void IAppRequest_CanBeUsedWithGenericConstraints()
    {
        // Arrange
        var processor = new RequestProcessor<TestAppRequest>();

        // Act
        var result = processor.Validate(new TestAppRequest { Name = "Valid" });

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IAppRequest<TResponse> Interface Tests

    [Fact]
    public void IAppRequestWithResponse_CanBeImplemented()
    {
        // Arrange & Act
        var request = new TestAppRequestWithResponse();

        // Assert
        request.Should().BeAssignableTo<IAppRequest<TestResponse>>();
    }

    [Fact]
    public void IAppRequestWithResponse_ImplementationCanContainProperties()
    {
        // Arrange & Act
        var request = new TestAppRequestWithResponse
        {
            Query = "SELECT * FROM table",
            MaxResults = 100
        };

        // Assert
        request.Query.Should().Be("SELECT * FROM table");
        request.MaxResults.Should().Be(100);
    }

    [Fact]
    public void IAppRequestWithResponse_CanBeUsedAsTypeParameter()
    {
        // Arrange
        var requests = new List<IAppRequest<TestResponse>>();

        // Act
        requests.Add(new TestAppRequestWithResponse());
        requests.Add(new AnotherRequestWithSameResponse());

        // Assert
        requests.Should().HaveCount(2);
        requests.Should().AllBeAssignableTo<IAppRequest<TestResponse>>();
    }

    [Fact]
    public void IAppRequestWithResponse_SupportsDifferentResponseTypes()
    {
        // Arrange & Act
        IAppRequest<TestResponse> request1 = new TestAppRequestWithResponse();
        IAppRequest<int> request2 = new RequestWithIntResponse();
        IAppRequest<string> request3 = new RequestWithStringResponse();

        // Assert
        request1.Should().BeAssignableTo<IAppRequest<TestResponse>>();
        request2.Should().BeAssignableTo<IAppRequest<int>>();
        request3.Should().BeAssignableTo<IAppRequest<string>>();
    }

    [Fact]
    public void IAppRequestWithResponse_CanBeUsedWithGenericConstraints()
    {
        // Arrange
        var handler = new GenericRequestHandler<TestAppRequestWithResponse, TestResponse>();

        // Act
        var result = handler.CanHandle(new TestAppRequestWithResponse());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IAppRequestWithResponse_ResponseTypeIsCovariant()
    {
        // Arrange
        var request = new RequestWithDerivedResponse();

        // Assert - Covariance allows IAppRequest<DerivedResponse> to be treated as IAppRequest<BaseResponse>
        request.Should().BeAssignableTo<IAppRequest<DerivedResponse>>();
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void IAppRequest_CanBeStoredInDictionary()
    {
        // Arrange
        var requestStore = new Dictionary<string, IAppRequest>();

        // Act
        requestStore["req1"] = new TestAppRequest { Id = 1 };
        requestStore["req2"] = new AnotherTestAppRequest { Code = "XYZ" };

        // Assert
        requestStore.Should().HaveCount(2);
        requestStore["req1"].Should().BeOfType<TestAppRequest>();
    }

    [Fact]
    public void IAppRequest_SupportsDerivedTypes()
    {
        // Arrange
        IAppRequest baseRequest = new DerivedTestAppRequest
        {
            Name = "Base",
            AdditionalInfo = "Extra"
        };

        // Assert
        baseRequest.Should().BeOfType<DerivedTestAppRequest>();
        ((DerivedTestAppRequest)baseRequest).AdditionalInfo.Should().Be("Extra");
    }

    [Fact]
    public void IAppRequestWithResponse_NullableResponseType()
    {
        // Arrange & Act
        var request = new RequestWithNullableResponse();

        // Assert
        request.Should().BeAssignableTo<IAppRequest<TestResponse?>>();
    }

    [Fact]
    public void IAppRequestWithResponse_CollectionResponseType()
    {
        // Arrange & Act
        var request = new RequestWithCollectionResponse();

        // Assert
        request.Should().BeAssignableTo<IAppRequest<List<TestResponse>>>();
    }

    #endregion

    #region Test Helper Classes

    public class TestAppRequest : IAppRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class AnotherTestAppRequest : IAppRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    public class DerivedTestAppRequest : TestAppRequest
    {
        public string AdditionalInfo { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Data { get; set; } = string.Empty;
    }

    public class BaseResponse
    {
        public int Id { get; set; }
    }

    public class DerivedResponse : BaseResponse
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestAppRequestWithResponse : IAppRequest<TestResponse>
    {
        public string Query { get; set; } = string.Empty;
        public int MaxResults { get; set; }
    }

    public class AnotherRequestWithSameResponse : IAppRequest<TestResponse>
    {
        public string Filter { get; set; } = string.Empty;
    }

    public class RequestWithIntResponse : IAppRequest<int>
    {
        public int Value { get; set; }
    }

    public class RequestWithStringResponse : IAppRequest<string>
    {
        public string Input { get; set; } = string.Empty;
    }

    public class RequestWithDerivedResponse : IAppRequest<DerivedResponse>
    {
        public int Id { get; set; }
    }

    public class RequestWithNullableResponse : IAppRequest<TestResponse?>
    {
        public bool ReturnNull { get; set; }
    }

    public class RequestWithCollectionResponse : IAppRequest<List<TestResponse>>
    {
        public int Count { get; set; }
    }

    public class RequestProcessor<T> where T : IAppRequest
    {
        public bool Validate(T request)
        {
            return request != null;
        }
    }

    public class GenericRequestHandler<TRequest, TResponse> where TRequest : IAppRequest<TResponse>
    {
        public bool CanHandle(TRequest request)
        {
            return request != null;
        }
    }

    #endregion
}
