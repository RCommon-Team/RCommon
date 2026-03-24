namespace RCommon.Blobs;

/// <summary>
/// Provider-agnostic interface for blob/object storage operations.
/// Covers container management, blob CRUD, metadata, transfer, and presigned URL generation.
/// </summary>
public interface IBlobStorageService
{
    // Container operations
    Task CreateContainerAsync(string containerName, CancellationToken token = default);
    Task DeleteContainerAsync(string containerName, CancellationToken token = default);
    Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default);
    Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default);

    // Blob CRUD
    Task UploadAsync(string containerName, string blobName, Stream content,
        BlobUploadOptions? options = null, CancellationToken token = default);
    Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default);
    Task DeleteAsync(string containerName, string blobName, CancellationToken token = default);
    Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default);
    Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName,
        string? prefix = null, CancellationToken token = default);

    // Metadata
    Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName,
        CancellationToken token = default);
    Task SetMetadataAsync(string containerName, string blobName,
        IDictionary<string, string> metadata, CancellationToken token = default);

    // Transfer
    Task CopyAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default);
    Task MoveAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default);

    // Presigned URLs
    Task<Uri> GetPresignedDownloadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default);
    Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default);
}
