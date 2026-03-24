namespace RCommon.Blobs;

/// <summary>
/// Detailed properties of a blob, returned from GetPropertiesAsync.
/// </summary>
public class BlobProperties
{
    public string ContentType { get; set; } = default!;
    public long ContentLength { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? ETag { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
