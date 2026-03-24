using Amazon.S3;
using FluentAssertions;
using RCommon.Amazon.S3Objects;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Amazon.S3Objects.Tests;

[Trait("Category", "Integration")]
public class AmazonS3StorageServiceTests : IAsyncLifetime
{
    private readonly AmazonS3StorageService _service;
    private readonly string _testBucket;

    public AmazonS3StorageServiceTests()
    {
        var client = new AmazonS3Client(
            "test", "test",
            new AmazonS3Config
            {
                ServiceURL = "http://localhost:4566",
                ForcePathStyle = true
            });
        _service = new AmazonS3StorageService(client);
        _testBucket = $"test-{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        await _service.CreateContainerAsync(_testBucket);
    }

    public async Task DisposeAsync()
    {
        try
        {
            var blobs = await _service.ListBlobsAsync(_testBucket);
            foreach (var blob in blobs)
                await _service.DeleteAsync(_testBucket, blob.Name);
            await _service.DeleteContainerAsync(_testBucket);
        }
        catch { }
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsTrue_ForExistingBucket()
    {
        var exists = await _service.ContainerExistsAsync(_testBucket);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsFalse_ForNonExistentBucket()
    {
        var exists = await _service.ContainerExistsAsync("nope-" + Guid.NewGuid());
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ListContainersAsync_IncludesTestBucket()
    {
        var buckets = await _service.ListContainersAsync();
        buckets.Should().Contain(_testBucket);
    }

    [Fact]
    public async Task UploadAndDownload_RoundTripsContent()
    {
        var content = "Hello, S3!"u8.ToArray();
        using var uploadStream = new MemoryStream(content);

        await _service.UploadAsync(_testBucket, "test.txt", uploadStream,
            new BlobUploadOptions { ContentType = "text/plain" });

        using var downloadStream = await _service.DownloadAsync(_testBucket, "test.txt");
        using var ms = new MemoryStream();
        await downloadStream.CopyToAsync(ms);
        ms.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_AfterUpload()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testBucket, "exists.txt", stream);
        (await _service.ExistsAsync(_testBucket, "exists.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_ForNonExistentObject()
    {
        (await _service.ExistsAsync(_testBucket, "nope.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesObject()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testBucket, "delete-me.txt", stream);
        await _service.DeleteAsync(_testBucket, "delete-me.txt");
        (await _service.ExistsAsync(_testBucket, "delete-me.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task ListBlobsAsync_ReturnsUploadedObjects()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        await _service.UploadAsync(_testBucket, "list/one.txt", s1);
        await _service.UploadAsync(_testBucket, "list/two.txt", s2);

        var blobs = (await _service.ListBlobsAsync(_testBucket, "list/")).ToList();
        blobs.Should().HaveCount(2);
        blobs.Select(b => b.Name).Should().Contain("list/one.txt").And.Contain("list/two.txt");
    }

    [Fact]
    public async Task GetPropertiesAsync_ReturnsObjectMetadata()
    {
        using var stream = new MemoryStream("props"u8.ToArray());
        await _service.UploadAsync(_testBucket, "props.txt", stream,
            new BlobUploadOptions
            {
                ContentType = "text/plain",
                Metadata = new Dictionary<string, string> { ["env"] = "test" }
            });

        var props = await _service.GetPropertiesAsync(_testBucket, "props.txt");
        props.ContentType.Should().Be("text/plain");
        props.ContentLength.Should().Be(5);
        props.Metadata.Should().ContainKey("env").WhoseValue.Should().Be("test");
    }

    [Fact]
    public async Task SetMetadataAsync_UpdatesMetadataViaCopyToSelf()
    {
        using var stream = new MemoryStream("m"u8.ToArray());
        await _service.UploadAsync(_testBucket, "meta.txt", stream);

        await _service.SetMetadataAsync(_testBucket, "meta.txt",
            new Dictionary<string, string> { ["key"] = "value" });

        var props = await _service.GetPropertiesAsync(_testBucket, "meta.txt");
        props.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public async Task CopyAsync_CopiesObject()
    {
        using var stream = new MemoryStream("copy"u8.ToArray());
        await _service.UploadAsync(_testBucket, "original.txt", stream);

        await _service.CopyAsync(_testBucket, "original.txt", _testBucket, "copy.txt");

        (await _service.ExistsAsync(_testBucket, "copy.txt")).Should().BeTrue();
        (await _service.ExistsAsync(_testBucket, "original.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_MovesObject()
    {
        using var stream = new MemoryStream("move"u8.ToArray());
        await _service.UploadAsync(_testBucket, "src.txt", stream);

        await _service.MoveAsync(_testBucket, "src.txt", _testBucket, "dest.txt");

        (await _service.ExistsAsync(_testBucket, "dest.txt")).Should().BeTrue();
        (await _service.ExistsAsync(_testBucket, "src.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ReturnsValidUri()
    {
        using var stream = new MemoryStream("signed"u8.ToArray());
        await _service.UploadAsync(_testBucket, "signed.txt", stream);

        var uri = await _service.GetPresignedDownloadUrlAsync(_testBucket, "signed.txt", TimeSpan.FromMinutes(5));
        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("signed.txt");
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_ReturnsValidUri()
    {
        var uri = await _service.GetPresignedUploadUrlAsync(_testBucket, "upload.txt", TimeSpan.FromMinutes(5));
        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("upload.txt");
    }
}
