namespace RCommon.Blobs;

/// <summary>
/// Options for upload operations.
/// </summary>
public class BlobUploadOptions
{
    public string? ContentType { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
    public bool Overwrite { get; set; } = true;
}
