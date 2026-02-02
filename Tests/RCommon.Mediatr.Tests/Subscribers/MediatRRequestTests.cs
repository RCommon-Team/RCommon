using FluentAssertions;
using MediatR;
using RCommon.MediatR.Subscribers;
using Xunit;

namespace RCommon.Mediatr.Tests.Subscribers;

public class MediatRRequestTests
{
    #region MediatRRequest<TRequest> Constructor Tests

    [Fact]
    public void MediatRRequest_Constructor_WithValidRequest_CreatesInstance()
    {
        // Arrange
        var request = new TestRequest { Data = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequest>(request);

        // Assert
        mediatRRequest.Should().NotBeNull();
    }

    [Fact]
    public void MediatRRequest_Constructor_StoresRequest()
    {
        // Arrange
        var request = new TestRequest { Data = "TestData" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequest>(request);

        // Assert
        mediatRRequest.Request.Should().BeSameAs(request);
        mediatRRequest.Request.Data.Should().Be("TestData");
    }

    [Fact]
    public void MediatRRequest_Constructor_WithNullRequest_StoresNull()
    {
        // Arrange & Act
        var mediatRRequest = new MediatRRequest<TestRequest>(null!);

        // Assert
        mediatRRequest.Request.Should().BeNull();
    }

    #endregion

    #region MediatRRequest<TRequest> Interface Implementation Tests

    [Fact]
    public void MediatRRequest_ImplementsIMediatRRequestOfT()
    {
        // Arrange
        var request = new TestRequest { Data = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequest>(request);

        // Assert
        mediatRRequest.Should().BeAssignableTo<IMediatRRequest<TestRequest>>();
    }

    [Fact]
    public void MediatRRequest_ImplementsIMediatRRequest()
    {
        // Arrange
        var request = new TestRequest { Data = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequest>(request);

        // Assert
        mediatRRequest.Should().BeAssignableTo<IMediatRRequest>();
    }

    [Fact]
    public void MediatRRequest_ImplementsIRequest()
    {
        // Arrange
        var request = new TestRequest { Data = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequest>(request);

        // Assert
        mediatRRequest.Should().BeAssignableTo<IRequest>();
    }

    #endregion

    #region MediatRRequest<TRequest> Request Property Tests

    [Fact]
    public void MediatRRequest_Request_IsReadOnly()
    {
        // Arrange
        var request = new TestRequest { Data = "Test" };
        var mediatRRequest = new MediatRRequest<TestRequest>(request);

        // Act & Assert
        var property = typeof(MediatRRequest<TestRequest>).GetProperty("Request");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.SetMethod.Should().BeNull();
    }

    #endregion

    #region MediatRRequest<TRequest, TResponse> Constructor Tests

    [Fact]
    public void MediatRRequestWithResponse_Constructor_WithValidRequest_CreatesInstance()
    {
        // Arrange
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mediatRRequest.Should().NotBeNull();
    }

    [Fact]
    public void MediatRRequestWithResponse_Constructor_StoresRequest()
    {
        // Arrange
        var request = new TestRequestWithResponse { Query = "TestQuery" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mediatRRequest.Request.Should().BeSameAs(request);
        mediatRRequest.Request.Query.Should().Be("TestQuery");
    }

    [Fact]
    public void MediatRRequestWithResponse_Constructor_WithNullRequest_StoresNull()
    {
        // Arrange & Act
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(null!);

        // Assert
        mediatRRequest.Request.Should().BeNull();
    }

    #endregion

    #region MediatRRequest<TRequest, TResponse> Interface Implementation Tests

    [Fact]
    public void MediatRRequestWithResponse_ImplementsIMediatRRequestOfTRequestTResponse()
    {
        // Arrange
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mediatRRequest.Should().BeAssignableTo<IMediatRRequest<TestRequestWithResponse, TestResponse>>();
    }

    [Fact]
    public void MediatRRequestWithResponse_ImplementsIMediatRRequestOfTResponse()
    {
        // Arrange
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mediatRRequest.Should().BeAssignableTo<IMediatRRequest<TestResponse>>();
    }

    [Fact]
    public void MediatRRequestWithResponse_ImplementsIMediatRRequest()
    {
        // Arrange
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mediatRRequest.Should().BeAssignableTo<IMediatRRequest>();
    }

    [Fact]
    public void MediatRRequestWithResponse_ImplementsIRequestOfTResponse()
    {
        // Arrange
        var request = new TestRequestWithResponse { Query = "Test" };

        // Act
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(request);

        // Assert
        mediatRRequest.Should().BeAssignableTo<IRequest<TestResponse>>();
    }

    #endregion

    #region MediatRRequest<TRequest, TResponse> Request Property Tests

    [Fact]
    public void MediatRRequestWithResponse_Request_IsReadOnly()
    {
        // Arrange
        var request = new TestRequestWithResponse { Query = "Test" };
        var mediatRRequest = new MediatRRequest<TestRequestWithResponse, TestResponse>(request);

        // Act & Assert
        var property = typeof(MediatRRequest<TestRequestWithResponse, TestResponse>).GetProperty("Request");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.SetMethod.Should().BeNull();
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public void MediatRRequest_WorksWithComplexTypes()
    {
        // Arrange
        var complexRequest = new ComplexRequest
        {
            Id = 42,
            Name = "ComplexRequest",
            Items = new List<string> { "Item1", "Item2", "Item3" }
        };

        // Act
        var mediatRRequest = new MediatRRequest<ComplexRequest>(complexRequest);

        // Assert
        mediatRRequest.Request.Should().NotBeNull();
        mediatRRequest.Request.Id.Should().Be(42);
        mediatRRequest.Request.Name.Should().Be("ComplexRequest");
        mediatRRequest.Request.Items.Should().HaveCount(3);
    }

    [Fact]
    public void MediatRRequestWithResponse_WorksWithComplexTypes()
    {
        // Arrange
        var complexRequest = new ComplexRequestWithResponse
        {
            Id = 42,
            Name = "ComplexRequest"
        };

        // Act
        var mediatRRequest = new MediatRRequest<ComplexRequestWithResponse, ComplexResponse>(complexRequest);

        // Assert
        mediatRRequest.Request.Should().NotBeNull();
        mediatRRequest.Request.Id.Should().Be(42);
        mediatRRequest.Request.Name.Should().Be("ComplexRequest");
    }

    #endregion

    #region Test Helper Classes

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

    public class ComplexRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }

    public class ComplexRequestWithResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ComplexResponse
    {
        public int ResultId { get; set; }
        public string ResultName { get; set; } = string.Empty;
    }

    #endregion
}
