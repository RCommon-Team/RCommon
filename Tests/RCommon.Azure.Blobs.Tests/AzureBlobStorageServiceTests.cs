using Azure.Storage.Blobs;
using FluentAssertions;
using RCommon.Azure.Blobs;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Azure.Blobs.Tests;

[Trait("Category", "Integration")]
public class AzureBlobStorageServiceTests : IAsyncLifetime
{
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private readonly AzureBlobStorageService _service;
    private readonly string _testContainer;

    public AzureBlobStorageServiceTests()
    {
        var client = new BlobServiceClient(ConnectionString);
        _service = new AzureBlobStorageService(client);
        _testContainer = $"test-{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        await _service.CreateContainerAsync(_testContainer);
    }

    public async Task DisposeAsync()
    {
        try { await _service.DeleteContainerAsync(_testContainer); } catch { }
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsTrue_ForExistingContainer()
    {
        var exists = await _service.ContainerExistsAsync(_testContainer);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsFalse_ForNonExistentContainer()
    {
        var exists = await _service.ContainerExistsAsync("does-not-exist-" + Guid.NewGuid());
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ListContainersAsync_IncludesTestContainer()
    {
        var containers = await _service.ListContainersAsync();
        containers.Should().Contain(_testContainer);
    }

    [Fact]
    public async Task UploadAndDownload_RoundTripsContent()
    {
        var content = "Hello, blob!"u8.ToArray();
        using var uploadStream = new MemoryStream(content);

        await _service.UploadAsync(_testContainer, "test.txt", uploadStream,
            new BlobUploadOptions { ContentType = "text/plain" });

        using var downloadStream = await _service.DownloadAsync(_testContainer, "test.txt");
        using var ms = new MemoryStream();
        await downloadStream.CopyToAsync(ms);
        ms.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_AfterUpload()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testContainer, "exists-test.txt", stream);

        var exists = await _service.ExistsAsync(_testContainer, "exists-test.txt");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_ForNonExistentBlob()
    {
        var exists = await _service.ExistsAsync(_testContainer, "nope.txt");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesBlob()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testContainer, "delete-me.txt", stream);

        await _service.DeleteAsync(_testContainer, "delete-me.txt");

        var exists = await _service.ExistsAsync(_testContainer, "delete-me.txt");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ListBlobsAsync_ReturnsUploadedBlobs()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        await _service.UploadAsync(_testContainer, "list/one.txt", s1);
        await _service.UploadAsync(_testContainer, "list/two.txt", s2);

        var blobs = (await _service.ListBlobsAsync(_testContainer, "list/")).ToList();
        blobs.Should().HaveCount(2);
        blobs.Select(b => b.Name).Should().Contain("list/one.txt").And.Contain("list/two.txt");
    }

    [Fact]
    public async Task GetPropertiesAsync_ReturnsMetadata()
    {
        using var stream = new MemoryStream("props"u8.ToArray());
        await _service.UploadAsync(_testContainer, "props.txt", stream,
            new BlobUploadOptions
            {
                ContentType = "text/plain",
                Metadata = new Dictionary<string, string> { ["env"] = "test" }
            });

        var props = await _service.GetPropertiesAsync(_testContainer, "props.txt");
        props.ContentType.Should().Be("text/plain");
        props.ContentLength.Should().Be(5);
        props.Metadata.Should().ContainKey("env").WhoseValue.Should().Be("test");
    }

    [Fact]
    public async Task SetMetadataAsync_UpdatesMetadata()
    {
        using var stream = new MemoryStream("m"u8.ToArray());
        await _service.UploadAsync(_testContainer, "meta.txt", stream);

        await _service.SetMetadataAsync(_testContainer, "meta.txt",
            new Dictionary<string, string> { ["key"] = "value" });

        var props = await _service.GetPropertiesAsync(_testContainer, "meta.txt");
        props.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public async Task CopyAsync_CopiesBlobToNewLocation()
    {
        using var stream = new MemoryStream("copy-me"u8.ToArray());
        await _service.UploadAsync(_testContainer, "original.txt", stream);

        await _service.CopyAsync(_testContainer, "original.txt", _testContainer, "copy.txt");

        var exists = await _service.ExistsAsync(_testContainer, "copy.txt");
        exists.Should().BeTrue();
        (await _service.ExistsAsync(_testContainer, "original.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_MovesBlob()
    {
        using var stream = new MemoryStream("move-me"u8.ToArray());
        await _service.UploadAsync(_testContainer, "move-src.txt", stream);

        await _service.MoveAsync(_testContainer, "move-src.txt", _testContainer, "move-dest.txt");

        (await _service.ExistsAsync(_testContainer, "move-dest.txt")).Should().BeTrue();
        (await _service.ExistsAsync(_testContainer, "move-src.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ReturnsValidUri()
    {
        using var stream = new MemoryStream("signed"u8.ToArray());
        await _service.UploadAsync(_testContainer, "signed.txt", stream);

        var uri = await _service.GetPresignedDownloadUrlAsync(_testContainer, "signed.txt", TimeSpan.FromMinutes(5));

        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("signed.txt");
        uri.AbsoluteUri.Should().Contain("sig=");
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_ReturnsValidUri()
    {
        var uri = await _service.GetPresignedUploadUrlAsync(_testContainer, "upload-target.txt", TimeSpan.FromMinutes(5));

        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("upload-target.txt");
        uri.AbsoluteUri.Should().Contain("sig=");
    }
}
