using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.Amazon.S3Objects;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Amazon.S3Objects.Tests;

public class AmazonS3ObjectsBuilderTests
{
    [Fact]
    public void AddBlobStore_WithExplicitCredentials_RegistersFactoryDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AmazonS3ObjectsBuilder>(blob =>
        {
            blob.AddBlobStore("test", options =>
            {
                options.AccessKeyId = "test";
                options.SecretAccessKey = "test";
                options.Region = "us-east-1";
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("test");
    }

    [Fact]
    public void AddBlobStore_WithDefaultCredentialChain_RegistersFactoryDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AmazonS3ObjectsBuilder>(blob =>
        {
            blob.AddBlobStore("default", options =>
            {
                options.ServiceUrl = "http://localhost:4566";
                options.ForcePathStyle = true;
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("default");
    }

    [Fact]
    public void AddBlobStore_MultipleStores_RegistersBothDelegates()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AmazonS3ObjectsBuilder>(blob =>
        {
            blob.AddBlobStore("primary", o => { o.Region = "us-east-1"; });
            blob.AddBlobStore("archive", o => { o.Region = "eu-west-1"; });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("primary").And.ContainKey("archive");
    }
}
