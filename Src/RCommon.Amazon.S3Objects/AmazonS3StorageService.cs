using Amazon.S3;
using Amazon.S3.Model;
using RCommon.Blobs;

namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Amazon S3 implementation of <see cref="IBlobStorageService"/>.
/// Wraps <see cref="IAmazonS3"/> from the AWSSDK.S3 package.
/// </summary>
public class AmazonS3StorageService : IBlobStorageService
{
    private readonly IAmazonS3 _client;

    public AmazonS3StorageService(IAmazonS3 client)
    {
        _client = client;
    }

    public async Task CreateContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.PutBucketAsync(containerName, token);
    }

    public async Task DeleteContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.DeleteBucketAsync(containerName, token);
    }

    public async Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default)
    {
        try
        {
            await _client.GetBucketLocationAsync(new GetBucketLocationRequest
            {
                BucketName = containerName
            }, token);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default)
    {
        var response = await _client.ListBucketsAsync(token);
        return response.Buckets.Select(b => b.BucketName);
    }

    public async Task UploadAsync(string containerName, string blobName, Stream content,
        BlobUploadOptions? options = null, CancellationToken token = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = containerName,
            Key = blobName,
            InputStream = content
        };

        if (options?.ContentType != null)
            request.ContentType = options.ContentType;

        if (options?.Metadata != null)
        {
            foreach (var kvp in options.Metadata)
                request.Metadata.Add(kvp.Key, kvp.Value);
        }

        await _client.PutObjectAsync(request, token);
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var response = await _client.GetObjectAsync(containerName, blobName, token);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken token = default)
    {
        await _client.DeleteObjectAsync(containerName, blobName, token);
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(containerName, blobName, token);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName,
        string? prefix = null, CancellationToken token = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = containerName,
            Prefix = prefix
        };

        var items = new List<BlobItem>();
        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request, token);
            foreach (var obj in response.S3Objects)
            {
                items.Add(new BlobItem
                {
                    Name = obj.Key,
                    Size = obj.Size,
                    LastModified = obj.LastModified
                });
            }
            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        return items;
    }

    public async Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName,
        CancellationToken token = default)
    {
        var response = await _client.GetObjectMetadataAsync(containerName, blobName, token);
        var metadata = new Dictionary<string, string>();
        foreach (var key in response.Metadata.Keys)
        {
            metadata[key] = response.Metadata[key];
        }

        return new BlobProperties
        {
            ContentType = response.Headers.ContentType,
            ContentLength = response.ContentLength,
            LastModified = response.LastModified,
            ETag = response.ETag,
            Metadata = metadata
        };
    }

    public async Task SetMetadataAsync(string containerName, string blobName,
        IDictionary<string, string> metadata, CancellationToken token = default)
    {
        // S3 does not support updating metadata in place — copy-to-self with REPLACE directive
        var copyRequest = new CopyObjectRequest
        {
            SourceBucket = containerName,
            SourceKey = blobName,
            DestinationBucket = containerName,
            DestinationKey = blobName,
            MetadataDirective = S3MetadataDirective.REPLACE
        };

        foreach (var kvp in metadata)
            copyRequest.Metadata.Add(kvp.Key, kvp.Value);

        await _client.CopyObjectAsync(copyRequest, token);
    }

    public async Task CopyAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default)
    {
        await _client.CopyObjectAsync(sourceContainer, sourceBlob, destContainer, destBlob, token);
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
        var request = new GetPreSignedUrlRequest
        {
            BucketName = containerName,
            Key = blobName,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }

    public Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = containerName,
            Key = blobName,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.PUT
        };

        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }
}
