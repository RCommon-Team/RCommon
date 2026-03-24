namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Configuration options for a named Amazon S3 blob store.
/// </summary>
public class AmazonS3StoreOptions
{
    public string? Region { get; set; }
    /// <summary>
    /// AWS credentials profile name. Uses CredentialProfileStoreChain for resolution.
    /// </summary>
    public string? Profile { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    /// <summary>
    /// Service URL for S3-compatible stores (MinIO, LocalStack).
    /// </summary>
    public string? ServiceUrl { get; set; }
    /// <summary>
    /// Required for S3-compatible stores that don't support virtual-hosted-style addressing.
    /// </summary>
    public bool ForcePathStyle { get; set; }
}
