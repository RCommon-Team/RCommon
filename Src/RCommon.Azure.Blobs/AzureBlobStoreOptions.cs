using Azure.Core;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Configuration options for a named Azure Blob Storage store.
/// Provide either <see cref="ConnectionString"/> or <see cref="ServiceUri"/> + <see cref="Credential"/>.
/// </summary>
public class AzureBlobStoreOptions
{
    public string? ConnectionString { get; set; }
    public Uri? ServiceUri { get; set; }
    public TokenCredential? Credential { get; set; }
}
