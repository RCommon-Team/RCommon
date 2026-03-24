namespace RCommon.Blobs;

/// <summary>
/// Thrown when a named blob store cannot be resolved from the factory.
/// </summary>
public class BlobStoreNotFoundException : Exception
{
    public BlobStoreNotFoundException(string message) : base(message) { }
}
