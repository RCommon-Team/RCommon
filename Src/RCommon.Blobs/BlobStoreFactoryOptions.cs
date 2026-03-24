using System.Collections.Concurrent;

namespace RCommon.Blobs;

/// <summary>
/// Configuration options for <see cref="BlobStoreFactory"/>.
/// Stores factory delegates keyed by blob store name.
/// </summary>
public class BlobStoreFactoryOptions
{
    public ConcurrentDictionary<string, Func<IServiceProvider, IBlobStorageService>> Stores { get; } = new();
}
