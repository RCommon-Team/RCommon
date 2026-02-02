using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Moq;
using RCommon.Authorization.Web.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Xunit;
using FilterDescriptor = Microsoft.AspNetCore.Mvc.Filters.FilterDescriptor;

namespace RCommon.Authorization.Web.Tests;

public class AuthorizeCheckOperationFilterTests
{
    [Fact]
    public void Apply_WhenNoAuthorizeAttribute_DoesNotModifyOperation()
    {
        // Arrange
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses(),
            Security = new List<OpenApiSecurityRequirement>()
        };
        var context = CreateOperationFilterContext(typeof(NonAuthorizedController), "PublicMethod");

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Responses.Should().BeEmpty();
        operation.Security.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WhenMethodHasAuthorizeAttribute_AddsUnauthorizedResponse()
    {
        // Arrange
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses(),
            Security = new List<OpenApiSecurityRequirement>()
        };
        var context = CreateOperationFilterContext(typeof(AuthorizedController), "AuthorizedMethod");

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Responses.Should().ContainKey("401");
        operation.Responses["401"].Description.Should().Be("Unauthorized");
    }

    [Fact]
    public void Apply_WhenMethodHasAuthorizeAttribute_AddsForbiddenResponse()
    {
        // Arrange
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses(),
            Security = new List<OpenApiSecurityRequirement>()
        };
        var context = CreateOperationFilterContext(typeof(AuthorizedController), "AuthorizedMethod");

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Responses.Should().ContainKey("403");
        operation.Responses["403"].Description.Should().Be("Forbidden");
    }

    [Fact]
    public void Apply_WhenControllerHasAuthorizeAttribute_AddsSecurityResponses()
    {
        // Arrange
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses(),
            Security = new List<OpenApiSecurityRequirement>()
        };
        var context = CreateOperationFilterContext(typeof(AuthorizedClassController), "AnyMethod");

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Responses.Should().ContainKey("401");
        operation.Responses.Should().ContainKey("403");
    }

    [Fact]
    public void Apply_WhenMethodHasAuthorizeAttribute_AddsSecurityRequirement()
    {
        // Arrange
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses(),
            Security = new List<OpenApiSecurityRequirement>()
        };
        var context = CreateOperationFilterContext(typeof(AuthorizedController), "AuthorizedMethod");

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Security.Should().NotBeEmpty();
    }

    [Fact]
    public void Apply_DoesNotDuplicateResponses()
    {
        // Arrange
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses
            {
                { "401", new OpenApiResponse { Description = "Existing" } }
            },
            Security = new List<OpenApiSecurityRequirement>()
        };
        var context = CreateOperationFilterContext(typeof(AuthorizedController), "AuthorizedMethod");

        // Act
        filter.Apply(operation, context);

        // Assert
        // TryAdd should not overwrite existing response
        operation.Responses["401"].Description.Should().Be("Existing");
    }

    [Fact]
    public void Filter_ImplementsIOperationFilter()
    {
        // Arrange & Act
        var filter = new AuthorizeCheckOperationFilter();

        // Assert
        filter.Should().BeAssignableTo<IOperationFilter>();
    }

    private OperationFilterContext CreateOperationFilterContext(Type controllerType, string methodName)
    {
        var methodInfo = controllerType.GetMethod(methodName)!;

        var actionDescriptor = new ActionDescriptor
        {
            FilterDescriptors = new List<FilterDescriptor>()
        };

        var apiDescription = new ApiDescription
        {
            ActionDescriptor = actionDescriptor
        };

        var schemaRepository = new SchemaRepository();
        var schemaGenerator = new Mock<ISchemaGenerator>();
        var openApiDocument = new OpenApiDocument();

        return new OperationFilterContext(
            apiDescription,
            schemaGenerator.Object,
            schemaRepository,
            openApiDocument,
            methodInfo);
    }

    // Test helper classes
    private class NonAuthorizedController
    {
        public void PublicMethod() { }
    }

    private class AuthorizedController
    {
        [Authorize]
        public void AuthorizedMethod() { }
    }

    [Authorize]
    private class AuthorizedClassController
    {
        public void AnyMethod() { }
    }
}
