using FluentAssertions;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Blobs.Tests;

public class BlobUploadOptionsTests
{
    [Fact]
    public void Overwrite_DefaultsToTrue()
    {
        var options = new BlobUploadOptions();
        options.Overwrite.Should().BeTrue();
    }

    [Fact]
    public void ContentType_DefaultsToNull()
    {
        var options = new BlobUploadOptions();
        options.ContentType.Should().BeNull();
    }

    [Fact]
    public void Metadata_DefaultsToNull()
    {
        var options = new BlobUploadOptions();
        options.Metadata.Should().BeNull();
    }
}
