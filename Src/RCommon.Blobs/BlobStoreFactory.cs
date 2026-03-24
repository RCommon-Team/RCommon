using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace RCommon.Blobs;

/// <summary>
/// Resolves named <see cref="IBlobStorageService"/> instances by invoking registered factory delegates.
/// Instances are created lazily on first resolution and cached for subsequent calls.
/// </summary>
public class BlobStoreFactory : IBlobStoreFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<string, Func<IServiceProvider, IBlobStorageService>> _stores;
    private readonly ConcurrentDictionary<string, IBlobStorageService> _instances = new();

    public BlobStoreFactory(IServiceProvider provider, IOptions<BlobStoreFactoryOptions> options)
    {
        _provider = provider;
        _stores = options.Value.Stores;
    }

    /// <inheritdoc />
    public IBlobStorageService Resolve(string name)
    {
        if (_instances.TryGetValue(name, out var cached))
            return cached;

        if (!_stores.TryGetValue(name, out var factory))
        {
            throw new BlobStoreNotFoundException(
                $"Blob store with name '{name}' not found.");
        }

        return _instances.GetOrAdd(name, _ => factory(_provider));
    }
}
