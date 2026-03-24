using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Blobs.Tests;

public class BlobStoreFactoryTests
{
    private readonly Mock<IServiceProvider> _serviceProvider;

    public BlobStoreFactoryTests()
    {
        _serviceProvider = new Mock<IServiceProvider>();
    }

    private BlobStoreFactory CreateFactory(BlobStoreFactoryOptions factoryOptions)
    {
        var options = Options.Create(factoryOptions);
        return new BlobStoreFactory(_serviceProvider.Object, options);
    }

    [Fact]
    public void Resolve_WithRegisteredName_ReturnsService()
    {
        var mockService = new Mock<IBlobStorageService>();
        var factoryOptions = new BlobStoreFactoryOptions();
        factoryOptions.Stores.TryAdd("test-store", sp => mockService.Object);
        var factory = CreateFactory(factoryOptions);

        var result = factory.Resolve("test-store");

        result.Should().BeSameAs(mockService.Object);
    }

    [Fact]
    public void Resolve_WithUnregisteredName_ThrowsBlobStoreNotFoundException()
    {
        var factory = CreateFactory(new BlobStoreFactoryOptions());

        var act = () => factory.Resolve("nonexistent");

        act.Should().Throw<BlobStoreNotFoundException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public void Resolve_CalledTwiceWithSameName_ReturnsCachedInstance()
    {
        var callCount = 0;
        var mockService = new Mock<IBlobStorageService>();
        var factoryOptions = new BlobStoreFactoryOptions();
        factoryOptions.Stores.TryAdd("cached", sp =>
        {
            callCount++;
            return mockService.Object;
        });
        var factory = CreateFactory(factoryOptions);

        var first = factory.Resolve("cached");
        var second = factory.Resolve("cached");

        first.Should().BeSameAs(second);
        callCount.Should().Be(1);
    }

    [Fact]
    public void Resolve_MultipleNames_ReturnsDistinctInstances()
    {
        var mockServiceA = new Mock<IBlobStorageService>();
        var mockServiceB = new Mock<IBlobStorageService>();
        var factoryOptions = new BlobStoreFactoryOptions();
        factoryOptions.Stores.TryAdd("store-a", sp => mockServiceA.Object);
        factoryOptions.Stores.TryAdd("store-b", sp => mockServiceB.Object);
        var factory = CreateFactory(factoryOptions);

        var resultA = factory.Resolve("store-a");
        var resultB = factory.Resolve("store-b");

        resultA.Should().BeSameAs(mockServiceA.Object);
        resultB.Should().BeSameAs(mockServiceB.Object);
        resultA.Should().NotBeSameAs(resultB);
    }
}
