using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RCommon.Json;
using RCommon.JsonNet;
using Xunit;

namespace RCommon.JsonNet.Tests;

public class IJsonNetBuilderExtensionsTests
{
    #region Configure Method Tests

    [Fact]
    public void Configure_WithValidAction_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        var act = () => builder.Configure(settings => { });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Configure_ReturnsBuilder_ForMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(settings => { });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_ReturnsIJsonNetBuilder_ForMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(settings => { });

        // Assert
        result.Should().BeAssignableTo<IJsonNetBuilder>();
    }

    [Fact]
    public void Configure_WithIndentedFormatting_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(settings =>
        {
            settings.Formatting = Formatting.Indented;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.Formatting.Should().Be(Formatting.Indented);
    }

    [Fact]
    public void Configure_WithNullValueHandling_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(settings =>
        {
            settings.NullValueHandling = NullValueHandling.Ignore;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.NullValueHandling.Should().Be(NullValueHandling.Ignore);
    }

    [Fact]
    public void Configure_WithCamelCaseContractResolver_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(settings =>
        {
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.ContractResolver.Should().BeOfType<CamelCasePropertyNamesContractResolver>();
    }

    [Fact]
    public void Configure_WithDateFormatString_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);
        var expectedFormat = "yyyy-MM-dd";

        // Act
        builder.Configure(settings =>
        {
            settings.DateFormatString = expectedFormat;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.DateFormatString.Should().Be(expectedFormat);
    }

    [Fact]
    public void Configure_WithReferenceLoopHandling_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(settings =>
        {
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.ReferenceLoopHandling.Should().Be(ReferenceLoopHandling.Ignore);
    }

    [Fact]
    public void Configure_WithMaxDepth_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);
        var expectedMaxDepth = 10;

        // Act
        builder.Configure(settings =>
        {
            settings.MaxDepth = expectedMaxDepth;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.MaxDepth.Should().Be(expectedMaxDepth);
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void Configure_CanBeChainedMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder
            .Configure(settings => settings.Formatting = Formatting.Indented)
            .Configure(settings => settings.NullValueHandling = NullValueHandling.Ignore);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_MultipleCalls_AllSettingsApplied()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        builder
            .Configure(settings => settings.Formatting = Formatting.Indented)
            .Configure(settings => settings.NullValueHandling = NullValueHandling.Ignore)
            .Configure(settings => settings.MaxDepth = 5);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.Formatting.Should().Be(Formatting.Indented);
        options.Value.NullValueHandling.Should().Be(NullValueHandling.Ignore);
        options.Value.MaxDepth.Should().Be(5);
    }

    [Fact]
    public void Configure_LastCallWins_ForSameSetting()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Act
        builder
            .Configure(settings => settings.MaxDepth = 5)
            .Configure(settings => settings.MaxDepth = 10)
            .Configure(settings => settings.MaxDepth = 15);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.MaxDepth.Should().Be(15);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Configure_WithJsonSerializer_AppliesSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Act
        builder.Configure(settings =>
        {
            settings.Formatting = Formatting.Indented;
        });
        var provider = services.BuildServiceProvider();
        var jsonSerializer = provider.GetRequiredService<IJsonSerializer>();
        var testObject = new { Name = "Test", Value = 123 };
        var json = jsonSerializer.Serialize(testObject);

        // Assert
        json.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void Configure_WithContractResolver_AffectsSerialization()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Act
        builder.Configure(settings =>
        {
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });
        var provider = services.BuildServiceProvider();
        var jsonSerializer = provider.GetRequiredService<IJsonSerializer>();
        var testObject = new TestPerson { FirstName = "John", LastName = "Doe" };
        var json = jsonSerializer.Serialize(testObject);

        // Assert
        json.Should().Contain("firstName");
        json.Should().Contain("lastName");
    }

    [Fact]
    public void Configure_WithNullValueHandling_AffectsSerialization()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Act
        builder.Configure(settings =>
        {
            settings.NullValueHandling = NullValueHandling.Ignore;
        });
        var provider = services.BuildServiceProvider();
        var jsonSerializer = provider.GetRequiredService<IJsonSerializer>();
        var testObject = new TestPerson { FirstName = "John", LastName = null! };
        var json = jsonSerializer.Serialize(testObject);

        // Assert
        json.Should().Contain("FirstName");
        json.Should().NotContain("LastName");
    }

    [Fact]
    public void Configure_WithDateFormat_AffectsSerialization()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Act
        builder.Configure(settings =>
        {
            settings.DateFormatString = "yyyy-MM-dd";
        });
        var provider = services.BuildServiceProvider();
        var jsonSerializer = provider.GetRequiredService<IJsonSerializer>();
        var testObject = new { Date = new DateTime(2024, 1, 15) };
        var json = jsonSerializer.Serialize(testObject);

        // Assert
        json.Should().Contain("2024-01-15");
    }

    #endregion

    #region Complex Configuration Tests

    [Fact]
    public void Configure_WithMultipleSettings_AllApplied()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Act
        builder.Configure(settings =>
        {
            settings.Formatting = Formatting.Indented;
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.DateFormatString = "yyyy-MM-dd";
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.MaxDepth = 32;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.Formatting.Should().Be(Formatting.Indented);
        options.Value.NullValueHandling.Should().Be(NullValueHandling.Ignore);
        options.Value.ContractResolver.Should().BeOfType<CamelCasePropertyNamesContractResolver>();
        options.Value.DateFormatString.Should().Be("yyyy-MM-dd");
        options.Value.ReferenceLoopHandling.Should().Be(ReferenceLoopHandling.Ignore);
        options.Value.MaxDepth.Should().Be(32);
    }

    [Fact]
    public void Configure_WithConverters_AddsConverters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);
        var customConverter = new Newtonsoft.Json.Converters.StringEnumConverter();

        // Act
        builder.Configure(settings =>
        {
            settings.Converters.Add(customConverter);
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.Converters.Should().Contain(customConverter);
    }

    [Fact]
    public void Configure_WithTypeNameHandling_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Act
        builder.Configure(settings =>
        {
            settings.TypeNameHandling = TypeNameHandling.Auto;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonSerializerSettings>>();

        // Assert
        options.Value.TypeNameHandling.Should().Be(TypeNameHandling.Auto);
    }

    #endregion

    #region Test Helper Classes

    private class TestPerson
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    #endregion
}
