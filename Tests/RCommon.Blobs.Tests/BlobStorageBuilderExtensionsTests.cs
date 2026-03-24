using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Blobs.Tests;

public class BlobStorageBuilderExtensionsTests
{
    [Fact]
    public void WithBlobStorage_RegistersBlobStoreFactoryAsSingleton()
    {
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });
        var provider = services.BuildServiceProvider();

        var factory1 = provider.GetService<IBlobStoreFactory>();
        var factory2 = provider.GetService<IBlobStoreFactory>();
        factory1.Should().NotBeNull();
        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void WithBlobStorage_CalledMultipleTimes_DoesNotDuplicateFactoryRegistration()
    {
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });
        builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });
        var provider = services.BuildServiceProvider();

        var factory = provider.GetService<IBlobStoreFactory>();
        factory.Should().NotBeNull();
        factory.Should().BeOfType<BlobStoreFactory>();
    }

    [Fact]
    public void WithBlobStorage_InvokesConfigurationAction()
    {
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);
        var actionCalled = false;

        builder.WithBlobStorage<TestBlobStorageBuilder>(b =>
        {
            actionCalled = true;
            b.Services.Should().BeSameAs(services);
        });

        actionCalled.Should().BeTrue();
    }

    [Fact]
    public void WithBlobStorage_ReturnsBuilder_ForFluentChaining()
    {
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        var result = builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });

        result.Should().BeSameAs(builder);
    }

    private class TestBlobStorageBuilder : IBlobStorageBuilder
    {
        public IServiceCollection Services { get; }

        public TestBlobStorageBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
        }
    }
}
