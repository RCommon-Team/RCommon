using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using RCommon.Blobs;
using RCommonBlobItem = RCommon.Blobs.BlobItem;
using RCommonBlobProperties = RCommon.Blobs.BlobProperties;
using RCommonBlobUploadOptions = RCommon.Blobs.BlobUploadOptions;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Wraps <see cref="BlobServiceClient"/> from the Azure.Storage.Blobs SDK.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(BlobServiceClient client)
    {
        _client = client;
    }

    public async Task CreateContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.CreateBlobContainerAsync(containerName, cancellationToken: token);
    }

    public async Task DeleteContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.DeleteBlobContainerAsync(containerName, cancellationToken: token);
    }

    public async Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var response = await container.ExistsAsync(token);
        return response.Value;
    }

    public async Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default)
    {
        var containers = new List<string>();
        await foreach (var item in _client.GetBlobContainersAsync(cancellationToken: token))
        {
            containers.Add(item.Name);
        }
        return containers;
    }

    public async Task UploadAsync(string containerName, string blobName, Stream content,
        RCommonBlobUploadOptions? options = null, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        var uploadOptions = new global::Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = options?.ContentType != null
                ? new BlobHttpHeaders { ContentType = options.ContentType }
                : null,
            Metadata = options?.Metadata,
        };

        if (options != null && !options.Overwrite)
        {
            uploadOptions.Conditions = new BlobRequestConditions
            {
                IfNoneMatch = global::Azure.ETag.All
            };
        }

        await blob.UploadAsync(content, uploadOptions, token);
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        return await blob.OpenReadAsync(cancellationToken: token);
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: token);
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        var response = await blob.ExistsAsync(token);
        return response.Value;
    }

    public async Task<IEnumerable<RCommonBlobItem>> ListBlobsAsync(string containerName,
        string? prefix = null, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var items = new List<RCommonBlobItem>();
        await foreach (var item in container.GetBlobsAsync(
            traits: BlobTraits.Metadata,
            prefix: prefix,
            cancellationToken: token))
        {
            items.Add(new RCommonBlobItem
            {
                Name = item.Name,
                Size = item.Properties.ContentLength,
                ContentType = item.Properties.ContentType,
                LastModified = item.Properties.LastModified,
                Metadata = item.Metadata ?? new Dictionary<string, string>()
            });
        }
        return items;
    }

    public async Task<RCommonBlobProperties> GetPropertiesAsync(string containerName, string blobName,
        CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        var response = await blob.GetPropertiesAsync(cancellationToken: token);
        var props = response.Value;

        return new RCommonBlobProperties
        {
            ContentType = props.ContentType,
            ContentLength = props.ContentLength,
            LastModified = props.LastModified,
            ETag = props.ETag.ToString(),
            Metadata = props.Metadata ?? new Dictionary<string, string>()
        };
    }

    public async Task SetMetadataAsync(string containerName, string blobName,
        IDictionary<string, string> metadata, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.SetMetadataAsync(metadata, cancellationToken: token);
    }

    public async Task CopyAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default)
    {
        var sourceClient = _client.GetBlobContainerClient(sourceContainer).GetBlobClient(sourceBlob);
        var destClient = _client.GetBlobContainerClient(destContainer).GetBlobClient(destBlob);

        var operation = await destClient.StartCopyFromUriAsync(sourceClient.Uri, cancellationToken: token);
        await operation.WaitForCompletionAsync(token);
    }

    public async Task MoveAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default)
    {
        await CopyAsync(sourceContainer, sourceBlob, destContainer, destBlob, token);
        await DeleteAsync(sourceContainer, sourceBlob, token);
    }

    public Task<Uri> GetPresignedDownloadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var uri = blob.GenerateSasUri(sasBuilder);
        return Task.FromResult(uri);
    }

    public Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var uri = blob.GenerateSasUri(sasBuilder);
        return Task.FromResult(uri);
    }
}
