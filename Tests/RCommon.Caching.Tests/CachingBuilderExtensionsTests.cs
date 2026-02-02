using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Caching;
using Xunit;

namespace RCommon.Caching.Tests;

public class CachingBuilderExtensionsTests
{
    #region Test Helper Classes

    public class TestMemoryCachingBuilder : IMemoryCachingBuilder
    {
        public TestMemoryCachingBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            WasConstructed = true;
        }

        public IServiceCollection Services { get; }

        public bool WasConstructed { get; }

        public bool ConfigurationActionWasCalled { get; set; }
    }

    public class TestDistributedCachingBuilder : IDistributedCachingBuilder
    {
        public TestDistributedCachingBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            WasConstructed = true;
        }

        public IServiceCollection Services { get; }

        public bool WasConstructed { get; }

        public bool ConfigurationActionWasCalled { get; set; }
    }

    #endregion

    #region WithMemoryCaching Tests

    [Fact]
    public void WithMemoryCaching_WithoutActions_CreatesBuilderInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var result = rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>();

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
    }

    [Fact]
    public void WithMemoryCaching_WithActions_InvokesConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        TestMemoryCachingBuilder? capturedBuilder = null;

        // Act
        var result = rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>(builder =>
        {
            capturedBuilder = builder;
            builder.ConfigurationActionWasCalled = true;
        });

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
        capturedBuilder.Should().NotBeNull();
        capturedBuilder!.WasConstructed.Should().BeTrue();
        capturedBuilder.ConfigurationActionWasCalled.Should().BeTrue();
    }

    [Fact]
    public void WithMemoryCaching_PassesCorrectServicesCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        IServiceCollection? capturedServices = null;

        // Act
        rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>(builder =>
        {
            capturedServices = builder.Services;
        });

        // Assert
        capturedServices.Should().BeSameAs(services);
    }

    [Fact]
    public void WithMemoryCaching_WithNullActions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        Action<TestMemoryCachingBuilder>? actions = null;

        // Act
        var act = () => rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>(actions!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithMemoryCaching_ReturnsOriginalBuilder_ForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var result = rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>(builder => { });

        // Assert
        result.Should().BeOfType<RCommonBuilder>();
        result.Should().BeSameAs(rcommonBuilder);
    }

    [Fact]
    public void WithMemoryCaching_NoActions_ReturnsOriginalBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var result = rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>();

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
    }

    #endregion

    #region WithDistributedCaching Tests

    [Fact]
    public void WithDistributedCaching_WithoutActions_CreatesBuilderInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var result = rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>();

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
    }

    [Fact]
    public void WithDistributedCaching_WithActions_InvokesConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        TestDistributedCachingBuilder? capturedBuilder = null;

        // Act
        var result = rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>(builder =>
        {
            capturedBuilder = builder;
            builder.ConfigurationActionWasCalled = true;
        });

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
        capturedBuilder.Should().NotBeNull();
        capturedBuilder!.WasConstructed.Should().BeTrue();
        capturedBuilder.ConfigurationActionWasCalled.Should().BeTrue();
    }

    [Fact]
    public void WithDistributedCaching_PassesCorrectServicesCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        IServiceCollection? capturedServices = null;

        // Act
        rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>(builder =>
        {
            capturedServices = builder.Services;
        });

        // Assert
        capturedServices.Should().BeSameAs(services);
    }

    [Fact]
    public void WithDistributedCaching_WithNullActions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        Action<TestDistributedCachingBuilder>? actions = null;

        // Act
        var act = () => rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>(actions!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithDistributedCaching_ReturnsOriginalBuilder_ForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var result = rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>(builder => { });

        // Assert
        result.Should().BeOfType<RCommonBuilder>();
        result.Should().BeSameAs(rcommonBuilder);
    }

    [Fact]
    public void WithDistributedCaching_NoActions_ReturnsOriginalBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var result = rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>();

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
    }

    #endregion

    #region Fluent Chaining Tests

    [Fact]
    public void FluentChaining_MemoryAndDistributedCaching_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        var memoryCachingConfigured = false;
        var distributedCachingConfigured = false;

        // Act
        var result = rcommonBuilder
            .WithMemoryCaching<TestMemoryCachingBuilder>(builder => memoryCachingConfigured = true)
            .WithDistributedCaching<TestDistributedCachingBuilder>(builder => distributedCachingConfigured = true);

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
        memoryCachingConfigured.Should().BeTrue();
        distributedCachingConfigured.Should().BeTrue();
    }

    [Fact]
    public void FluentChaining_WithOtherBuilderMethods_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var result = rcommonBuilder
            .WithSimpleGuidGenerator()
            .WithMemoryCaching<TestMemoryCachingBuilder>(builder => { })
            .WithDateTimeSystem(options => { });

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
    }

    #endregion

    #region Builder Instance Creation Tests

    [Fact]
    public void WithMemoryCaching_CreatesNewBuilderInstanceEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        TestMemoryCachingBuilder? firstBuilder = null;
        TestMemoryCachingBuilder? secondBuilder = null;

        // Act
        rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>(builder => firstBuilder = builder);
        rcommonBuilder.WithMemoryCaching<TestMemoryCachingBuilder>(builder => secondBuilder = builder);

        // Assert
        firstBuilder.Should().NotBeNull();
        secondBuilder.Should().NotBeNull();
        firstBuilder.Should().NotBeSameAs(secondBuilder);
    }

    [Fact]
    public void WithDistributedCaching_CreatesNewBuilderInstanceEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        TestDistributedCachingBuilder? firstBuilder = null;
        TestDistributedCachingBuilder? secondBuilder = null;

        // Act
        rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>(builder => firstBuilder = builder);
        rcommonBuilder.WithDistributedCaching<TestDistributedCachingBuilder>(builder => secondBuilder = builder);

        // Assert
        firstBuilder.Should().NotBeNull();
        secondBuilder.Should().NotBeNull();
        firstBuilder.Should().NotBeSameAs(secondBuilder);
    }

    #endregion
}
