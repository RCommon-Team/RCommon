namespace RCommon.Blobs;

/// <summary>
/// Resolves named <see cref="IBlobStorageService"/> instances from registered blob stores.
/// </summary>
public interface IBlobStoreFactory
{
    /// <summary>
    /// Resolves a blob storage service by its registered name.
    /// </summary>
    IBlobStorageService Resolve(string name);
}
