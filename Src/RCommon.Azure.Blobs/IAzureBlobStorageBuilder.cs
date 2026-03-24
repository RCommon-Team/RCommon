using RCommon.Blobs;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Builder interface for configuring Azure Blob Storage as an <see cref="IBlobStorageService"/> provider.
/// </summary>
public interface IAzureBlobStorageBuilder : IBlobStorageBuilder
{
    /// <summary>
    /// Registers a named Azure blob store with the specified options.
    /// </summary>
    IAzureBlobStorageBuilder AddBlobStore(string name, Action<AzureBlobStoreOptions> options);
}
