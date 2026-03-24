using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.Azure.Blobs;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Azure.Blobs.Tests;

public class AzureBlobStorageBuilderTests
{
    [Fact]
    public void AddBlobStore_WithConnectionString_RegistersFactoryDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AzureBlobStorageBuilder>(blob =>
        {
            blob.AddBlobStore("test", options =>
            {
                options.ConnectionString = "UseDevelopmentStorage=true";
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("test");
    }

    [Fact]
    public void AddBlobStore_WithoutConnectionStringOrCredential_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        builder.WithBlobStorage<AzureBlobStorageBuilder>(blob =>
        {
            blob.AddBlobStore("invalid", options => { });
        });
        var provider = services.BuildServiceProvider();

        // Act
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        var factory = factoryOptions.Value.Stores["invalid"];
        var act = () => factory(provider);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires either ConnectionString or ServiceUri*");
    }

    [Fact]
    public void AddBlobStore_MultipleStores_RegistersBothDelegates()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AzureBlobStorageBuilder>(blob =>
        {
            blob.AddBlobStore("primary", o => o.ConnectionString = "UseDevelopmentStorage=true");
            blob.AddBlobStore("backup", o => o.ConnectionString = "UseDevelopmentStorage=true");
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("primary").And.ContainKey("backup");
    }
}
