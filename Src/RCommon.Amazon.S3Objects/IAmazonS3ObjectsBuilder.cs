using RCommon.Blobs;

namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Builder interface for configuring Amazon S3 as an <see cref="IBlobStorageService"/> provider.
/// </summary>
public interface IAmazonS3ObjectsBuilder : IBlobStorageBuilder
{
    /// <summary>
    /// Registers a named S3 blob store with the specified options.
    /// </summary>
    IAmazonS3ObjectsBuilder AddBlobStore(string name, Action<AmazonS3StoreOptions> options);
}
