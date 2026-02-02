using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Json;
using RCommon.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace RCommon.SystemTextJson.Tests;

public class ITextJsonBuilderExtensionsTests
{
    #region Configure Extension Method Tests

    [Fact]
    public void Configure_WithOptions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(options => { });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_WithOptions_ConfiguresJsonSerializerOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options =>
        {
            options.WriteIndented = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.WriteIndented.Should().BeTrue();
        configuredOptions.Value.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void Configure_WithWriteIndented_SetsWriteIndented()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => options.WriteIndented = true);
        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.WriteIndented.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithPropertyNamingPolicy_SetsPropertyNamingPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void Configure_WithPropertyNameCaseInsensitive_SetsPropertyNameCaseInsensitive()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => options.PropertyNameCaseInsensitive = true);
        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.PropertyNameCaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithConverter_AddsConverter()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => options.Converters.Add(new JsonStringEnumConverter()));
        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.Converters.Should().ContainSingle();
        configuredOptions.Value.Converters.First().Should().BeOfType<JsonStringEnumConverter>();
    }

    [Fact]
    public void Configure_WithMultipleConverters_AddsAllConverters()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options =>
        {
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new JsonIntEnumConverter<TestEnum>());
        });
        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.Converters.Should().HaveCount(2);
    }

    [Fact]
    public void Configure_WithDefaultIgnoreCondition_SetsDefaultIgnoreCondition()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void Configure_WithMaxDepth_SetsMaxDepth()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => options.MaxDepth = 100);
        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.MaxDepth.Should().Be(100);
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Configure_CanBeChained_WithMultipleCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder
            .Configure(options => options.WriteIndented = true)
            .Configure(options => options.PropertyNameCaseInsensitive = true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_ChainedCalls_ApplyAllConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        builder
            .Configure(options => options.WriteIndented = true)
            .Configure(options => options.PropertyNameCaseInsensitive = true);

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        // Assert
        configuredOptions.Value.WriteIndented.Should().BeTrue();
        configuredOptions.Value.PropertyNameCaseInsensitive.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Configure_ConfiguredOptions_UsedByTextJsonSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        builder.Configure(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.WriteIndented = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var serializer = serviceProvider.GetRequiredService<IJsonSerializer>();
        var testObj = new TestObject { Name = "Test", Value = 42 };

        // Act
        var json = serializer.Serialize(testObj);

        // Assert
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"value\"");
        json.Should().Contain("\n");
    }

    [Fact]
    public void Configure_WithCustomConverter_SerializerUsesConverter()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        builder.Configure(options =>
        {
            options.Converters.Add(new JsonIntEnumConverter<TestEnum>());
        });

        var serviceProvider = services.BuildServiceProvider();
        var serializer = serviceProvider.GetRequiredService<IJsonSerializer>();
        var testObj = new TestObjectWithEnum { Status = TestEnum.Active };

        // Act
        var json = serializer.Serialize(testObj);

        // Assert
        json.Should().Contain("1"); // Active = 1 as integer
    }

    #endregion

    #region Type Tests

    [Fact]
    public void Configure_ReturnsITextJsonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(options => { });

        // Assert
        result.Should().BeAssignableTo<ITextJsonBuilder>();
    }

    #endregion

    #region Test Helper Classes

    public enum TestEnum
    {
        None = 0,
        Active = 1,
        Inactive = 2
    }

    public class TestObject
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public class TestObjectWithEnum
    {
        public TestEnum Status { get; set; }
    }

    #endregion
}
