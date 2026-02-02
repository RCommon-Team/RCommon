using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OpenApi;
using Moq;
using RCommon.Authorization.Web.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;
using FilterDescriptor = Microsoft.AspNetCore.Mvc.Filters.FilterDescriptor;
using FilterScope = Microsoft.AspNetCore.Mvc.Filters.FilterScope;

namespace RCommon.Authorization.Web.Tests;

public class AuthorizationHeaderParameterOperationFilterTests
{
    [Fact]
    public void Apply_WhenNotAuthorized_DoesNotAddParameter()
    {
        // Arrange
        var filter = new AuthorizationHeaderParameterOperationFilter();
        var operation = new OpenApiOperation { Parameters = [] };
        var context = CreateOperationFilterContext(hasAuthorizeFilter: false, hasAllowAnonymous: false);

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WhenAuthorizedButAllowAnonymous_DoesNotAddParameter()
    {
        // Arrange
        var filter = new AuthorizationHeaderParameterOperationFilter();
        var operation = new OpenApiOperation { Parameters = [] };
        var context = CreateOperationFilterContext(hasAuthorizeFilter: true, hasAllowAnonymous: true);

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WhenAuthorizedAndNotAllowAnonymous_AddsAuthorizationParameter()
    {
        // Arrange
        var filter = new AuthorizationHeaderParameterOperationFilter();
        var operation = new OpenApiOperation { Parameters = null };
        var context = CreateOperationFilterContext(hasAuthorizeFilter: true, hasAllowAnonymous: false);

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Parameters.Should().NotBeNull();
        operation.Parameters.Should().HaveCount(1);
        var param = operation.Parameters.First();
        param.Name.Should().Be("Authorization");
        param.In.Should().Be(ParameterLocation.Header);
        param.Required.Should().BeTrue();
        param.Description.Should().Be("access token");
    }

    [Fact]
    public void Apply_WhenParametersIsNull_CreatesNewList()
    {
        // Arrange
        var filter = new AuthorizationHeaderParameterOperationFilter();
        var operation = new OpenApiOperation { Parameters = null };
        var context = CreateOperationFilterContext(hasAuthorizeFilter: true, hasAllowAnonymous: false);

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Parameters.Should().NotBeNull();
    }

    [Fact]
    public void Apply_WhenParametersExists_AppendsToList()
    {
        // Arrange
        var filter = new AuthorizationHeaderParameterOperationFilter();
        var existingParam = new OpenApiParameter { Name = "Existing" };
        var operation = new OpenApiOperation { Parameters = [existingParam] };
        var context = CreateOperationFilterContext(hasAuthorizeFilter: true, hasAllowAnonymous: false);

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Parameters.Should().HaveCount(2);
        operation.Parameters.Should().Contain(existingParam);
    }

    [Fact]
    public void Filter_ImplementsIOperationFilter()
    {
        // Arrange & Act
        var filter = new AuthorizationHeaderParameterOperationFilter();

        // Assert
        filter.Should().BeAssignableTo<IOperationFilter>();
    }

    private OperationFilterContext CreateOperationFilterContext(bool hasAuthorizeFilter, bool hasAllowAnonymous)
    {
        var filterDescriptors = new List<FilterDescriptor>();

        if (hasAuthorizeFilter)
        {
            var authFilter = new AuthorizeFilter();
            filterDescriptors.Add(new FilterDescriptor(authFilter, (int)FilterScope.Controller));
        }

        if (hasAllowAnonymous)
        {
            var anonFilter = new Mock<IAllowAnonymousFilter>();
            filterDescriptors.Add(new FilterDescriptor(anonFilter.Object, (int)FilterScope.Action));
        }

        var actionDescriptor = new ActionDescriptor
        {
            FilterDescriptors = filterDescriptors
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
            typeof(object).GetMethod("ToString")!);
    }
}
