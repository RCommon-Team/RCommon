namespace RCommon.Blobs;

/// <summary>
/// Represents a blob item returned from listing operations.
/// </summary>
public class BlobItem
{
    public string Name { get; set; } = default!;
    public long? Size { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
