# Blob Storage Abstractions — Design Specification

**Date:** 2026-03-23
**Branch:** feature/blob-abstractions
**Status:** Design

## Problem

RCommon provides provider-agnostic abstractions for persistence, caching, emailing, and messaging, but has no abstraction for blob/object storage. Applications that need to store and retrieve files (images, documents, backups, exports) must depend directly on Azure Blob Storage or AWS S3 SDKs, coupling application code to a specific cloud provider. There is also no support for working with multiple blob stores simultaneously (e.g. reading from S3 and writing to Azure, or using multiple buckets/containers within the same provider).

## Goal

Add a provider-agnostic blob storage abstraction to RCommon with two initial implementations:

1. **RCommon.Blobs** — Core abstraction library defining `IBlobStorageService`, multi-store factory, builder infrastructure, and supporting types.
2. **RCommon.Azure.Blobs** — Azure Blob Storage implementation wrapping `Azure.Storage.Blobs`.
3. **RCommon.Amazon.S3Objects** — AWS S3 implementation wrapping `AWSSDK.S3`.

The abstraction supports the full lifecycle: container management, blob CRUD, metadata, copy/move, and presigned URL generation for both upload and download. Multi-store support via `IBlobStoreFactory` enables applications to work with multiple named blob stores across different providers simultaneously.

## Non-Goals

- Streaming/chunked multipart upload (V2 enhancement)
- Event notifications on blob operations (V2 enhancement)
- Local filesystem implementation for development (V2 enhancement)
- Retry policies (consumers use Polly or SDK-level retries)
- Blob-level access control / permissions management
- CDN integration

---

## Core Abstraction — RCommon.Blobs

### IBlobStorageService

**Location:** `Src/RCommon.Blobs/IBlobStorageService.cs`
**Namespace:** `RCommon.Blobs`

The single unified interface covering all blob storage operations. All operations are async with `CancellationToken` support.

```csharp
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
```

**API decisions:**

- **Unified interface** — All operations on one interface. The operations are cohesive (all relate to blob storage) and this matches existing RCommon patterns (`ICacheService`, `IEmailService`).
- **`containerName` + `blobName` parameters** — Two-level addressing is the universal model across Azure (container/blob) and S3 (bucket/key). Using string parameters rather than a typed container reference keeps the API simple and consistent with how both SDKs work.
- **`BlobUploadOptions` is optional** — Defaults to sensible values (auto-detect content type, allow overwrite). Keeps the common case simple.
- **`Stream` for upload/download** — Standard .NET pattern for binary data. Callers manage stream lifecycle.
- **`MoveAsync`** — Neither Azure nor S3 has a native move operation. Both implementations will copy + delete. This is documented as a non-atomic operation.
- **`ListBlobsAsync` with prefix** — S3 and Azure both support prefix-based listing natively. This enables hierarchical "folder-like" browsing without adding a separate folder abstraction.

### Supporting Types

**Location:** `Src/RCommon.Blobs/`

```csharp
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

/// <summary>
/// Options for upload operations.
/// </summary>
public class BlobUploadOptions
{
    public string? ContentType { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
    public bool Overwrite { get; set; } = true;
}
```

### Multi-Store Factory

**Location:** `Src/RCommon.Blobs/`

Unlike the persistence `IDataStoreFactory` (which resolves by concrete type from DI), the blob factory uses a **name-to-factory-delegate dictionary**. This solves a fundamental problem: multiple blob stores of the same provider (e.g. two Azure stores with different connection strings) cannot be disambiguated by `GetRequiredService(typeof(AzureBlobStorageService))` since DI resolves a single registration per type. Instead, each `AddBlobStore` call registers a `Func<IServiceProvider, IBlobStorageService>` factory delegate keyed by name.

```csharp
public interface IBlobStoreFactory
{
    IBlobStorageService Resolve(string name);
}

public class BlobStoreFactoryOptions
{
    public ConcurrentDictionary<string, Func<IServiceProvider, IBlobStorageService>> Stores { get; } = new();
}

public class BlobStoreFactory : IBlobStoreFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<string, Func<IServiceProvider, IBlobStorageService>> _stores;
    private readonly ConcurrentDictionary<string, IBlobStorageService> _instances = new();

    public BlobStoreFactory(IServiceProvider provider, IOptions<BlobStoreFactoryOptions> options)
    {
        _provider = provider;
        _stores = options.Value.Stores;
    }

    public IBlobStorageService Resolve(string name)
    {
        // Lazy-create and cache instances per name
        return _instances.GetOrAdd(name, key =>
        {
            if (!_stores.TryGetValue(key, out var factory))
            {
                throw new BlobStoreNotFoundException(
                    $"Blob store with name '{key}' not found.");
            }

            return factory(_provider);
        });
    }
}

public class BlobStoreNotFoundException : Exception
{
    public BlobStoreNotFoundException(string message) : base(message) { }
}
```

**Design decisions:**
- **Factory delegate per name** — Each `AddBlobStore` call registers a `Func<IServiceProvider, IBlobStorageService>` under its name. This allows the same concrete type (e.g. `AzureBlobStorageService`) to be registered multiple times with different configurations (different connection strings, regions, etc.).
- **Lazy instance caching** — `BlobStoreFactory` caches resolved instances in a `ConcurrentDictionary<string, IBlobStorageService>`. The factory delegate is invoked once per name, and the resulting instance is reused. Both Azure `BlobServiceClient` and AWS `AmazonS3Client` are designed to be long-lived singletons.
- **`ConcurrentDictionary` over `ConcurrentBag`** — Name-based lookup is O(1) instead of linear scan, and dictionary semantics naturally enforce unique names.
- **No `BlobStoreValue` type needed** — The factory delegate replaces the need for a separate value type mapping names to concrete types.

### Builder Infrastructure

**Location:** `Src/RCommon.Blobs/`

```csharp
/// <summary>
/// Base builder interface for configuring blob storage providers.
/// </summary>
public interface IBlobStorageBuilder
{
    IServiceCollection Services { get; }
}
```

**Location:** `Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs`

```csharp
public static class BlobStorageBuilderExtensions
{
    public static IRCommonBuilder WithBlobStorage<T>(this IRCommonBuilder builder)
        where T : IBlobStorageBuilder
    {
        return WithBlobStorage<T>(builder, x => { });
    }

    public static IRCommonBuilder WithBlobStorage<T>(this IRCommonBuilder builder, Action<T> actions)
        where T : IBlobStorageBuilder
    {
        Guard.IsNotNull(actions, nameof(actions));

        // Register factory infrastructure (idempotent — safe to call multiple times)
        builder.Services.TryAddSingleton<IBlobStoreFactory, BlobStoreFactory>();

        var blobConfig = (T)(Activator.CreateInstance(typeof(T), new object[] { builder })
            ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}."));
        actions(blobConfig);
        return builder;
    }
}
```

**Key detail:** `WithBlobStorage` can be called multiple times (once per provider) because the factory and options registrations use `TryAddSingleton` — the first call registers them, subsequent calls are no-ops. Each call creates a new provider-specific builder that adds its own named stores to the shared `BlobStoreFactoryOptions`.

---

## Azure Implementation — RCommon.Azure.Blobs

### Builder

**Location:** `Src/RCommon.Azure.Blobs/`

```csharp
public interface IAzureBlobStorageBuilder : IBlobStorageBuilder
{
    IAzureBlobStorageBuilder AddBlobStore(string name, Action<AzureBlobStoreOptions> options);
}

public class AzureBlobStoreOptions
{
    public string? ConnectionString { get; set; }
    public Uri? ServiceUri { get; set; }
    public TokenCredential? Credential { get; set; }
}

public class AzureBlobStorageBuilder : IAzureBlobStorageBuilder
{
    public IServiceCollection Services { get; }

    public AzureBlobStorageBuilder(IRCommonBuilder builder)
    {
        Services = builder.Services;
    }

    public IAzureBlobStorageBuilder AddBlobStore(string name, Action<AzureBlobStoreOptions> options)
    {
        var storeOptions = new AzureBlobStoreOptions();
        options(storeOptions);

        // Register a factory delegate keyed by name
        Services.Configure<BlobStoreFactoryOptions>(o =>
            o.Stores.TryAdd(name, sp =>
            {
                BlobServiceClient client;
                if (storeOptions.ConnectionString != null)
                {
                    client = new BlobServiceClient(storeOptions.ConnectionString);
                }
                else if (storeOptions.ServiceUri != null && storeOptions.Credential != null)
                {
                    client = new BlobServiceClient(storeOptions.ServiceUri, storeOptions.Credential);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Blob store '{name}' requires either ConnectionString or ServiceUri + Credential.");
                }

                return new AzureBlobStorageService(client);
            }));

        return this;
    }
}
```

Each `AddBlobStore` call registers a factory delegate under the given name. The `BlobStoreFactory` invokes the delegate once per name and caches the result. This supports multiple Azure stores with different connection strings without DI type conflicts.

### Service Implementation

**Location:** `Src/RCommon.Azure.Blobs/AzureBlobStorageService.cs`

```csharp
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(BlobServiceClient client)
    {
        _client = client;
    }

    // Implementation delegates to Azure SDK...
}
```

**Azure SDK mapping:**

| Abstraction | Azure SDK |
|------------|-----------|
| `CreateContainerAsync` | `BlobServiceClient.CreateBlobContainerAsync` |
| `DeleteContainerAsync` | `BlobServiceClient.DeleteBlobContainerAsync` |
| `ContainerExistsAsync` | `BlobContainerClient.ExistsAsync` |
| `ListContainersAsync` | `BlobServiceClient.GetBlobContainersAsync` |
| `UploadAsync` | `BlobClient.UploadAsync` with `BlobUploadOptions` → `BlobHttpHeaders` |
| `DownloadAsync` | `BlobClient.OpenReadAsync` |
| `DeleteAsync` | `BlobClient.DeleteIfExistsAsync` |
| `ExistsAsync` | `BlobClient.ExistsAsync` |
| `ListBlobsAsync` | `BlobContainerClient.GetBlobsAsync` with prefix |
| `GetPropertiesAsync` | `BlobClient.GetPropertiesAsync` |
| `SetMetadataAsync` | `BlobClient.SetMetadataAsync` |
| `CopyAsync` | `BlobClient.StartCopyFromUriAsync` |
| `MoveAsync` | Copy + `BlobClient.DeleteIfExistsAsync` (non-atomic) |
| `GetPresignedDownloadUrlAsync` | `BlobSasBuilder` with `BlobSasPermissions.Read` |
| `GetPresignedUploadUrlAsync` | `BlobSasBuilder` with `BlobSasPermissions.Write` |

**NuGet dependency:** `Azure.Storage.Blobs`

**Presigned URL prerequisite:** SAS token generation requires either a `StorageSharedKeyCredential` (from connection string) or user delegation key (from `TokenCredential`). The implementation will extract the shared key from the connection string when available, or use `BlobServiceClient.GetUserDelegationKeyAsync` for token-credential-based accounts.

---

## Amazon S3 Implementation — RCommon.Amazon.S3Objects

### Builder

**Location:** `Src/RCommon.Amazon.S3Objects/`

```csharp
public interface IAmazonS3ObjectsBuilder : IBlobStorageBuilder
{
    IAmazonS3ObjectsBuilder AddBlobStore(string name, Action<AmazonS3StoreOptions> options);
}

public class AmazonS3StoreOptions
{
    public string? Region { get; set; }
    public string? Profile { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? ServiceUrl { get; set; }  // For S3-compatible stores (MinIO, LocalStack)
    public bool ForcePathStyle { get; set; }  // Required for S3-compatible stores
}

public class AmazonS3ObjectsBuilder : IAmazonS3ObjectsBuilder
{
    public IServiceCollection Services { get; }

    public AmazonS3ObjectsBuilder(IRCommonBuilder builder)
    {
        Services = builder.Services;
    }

    public IAmazonS3ObjectsBuilder AddBlobStore(string name, Action<AmazonS3StoreOptions> options)
    {
        var storeOptions = new AmazonS3StoreOptions();
        options(storeOptions);

        // Register a factory delegate keyed by name
        Services.Configure<BlobStoreFactoryOptions>(o =>
            o.Stores.TryAdd(name, sp =>
            {
                var config = new AmazonS3Config();
                if (storeOptions.Region != null)
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(storeOptions.Region);
                if (storeOptions.ServiceUrl != null)
                    config.ServiceURL = storeOptions.ServiceUrl;
                config.ForcePathStyle = storeOptions.ForcePathStyle;

                IAmazonS3 client;
                if (storeOptions.AccessKeyId != null && storeOptions.SecretAccessKey != null)
                {
                    client = new AmazonS3Client(
                        storeOptions.AccessKeyId, storeOptions.SecretAccessKey, config);
                }
                else
                {
                    // Falls back to default credential chain (env vars, instance profile, etc.)
                    client = new AmazonS3Client(config);
                }

                return new AmazonS3StorageService(client);
            }));

        return this;
    }
}
```

### Service Implementation

**Location:** `Src/RCommon.Amazon.S3Objects/AmazonS3StorageService.cs`

```csharp
public class AmazonS3StorageService : IBlobStorageService
{
    private readonly IAmazonS3 _client;

    public AmazonS3StorageService(IAmazonS3 client)
    {
        _client = client;
    }

    // Implementation delegates to AWS SDK...
}
```

**AWS SDK mapping:**

| Abstraction | AWS SDK |
|------------|---------|
| `CreateContainerAsync` | `PutBucketAsync` |
| `DeleteContainerAsync` | `DeleteBucketAsync` |
| `ContainerExistsAsync` | `DoesS3BucketExistV2Async` (extension method) |
| `ListContainersAsync` | `ListBucketsAsync` |
| `UploadAsync` | `PutObjectAsync` with `PutObjectRequest` |
| `DownloadAsync` | `GetObjectAsync` → `GetObjectResponse.ResponseStream` |
| `DeleteAsync` | `DeleteObjectAsync` |
| `ExistsAsync` | `GetObjectMetadataAsync` (catch `AmazonS3Exception` with 404) |
| `ListBlobsAsync` | `ListObjectsV2Async` with `Prefix` |
| `GetPropertiesAsync` | `GetObjectMetadataAsync` |
| `SetMetadataAsync` | `CopyObjectAsync` with `MetadataDirective.REPLACE` (S3 requires copy-to-self to update metadata) |
| `CopyAsync` | `CopyObjectAsync` |
| `MoveAsync` | `CopyObjectAsync` + `DeleteObjectAsync` (non-atomic) |
| `GetPresignedDownloadUrlAsync` | `GetPreSignedURL` with `HttpVerb.GET` |
| `GetPresignedUploadUrlAsync` | `GetPreSignedURL` with `HttpVerb.PUT` |

**NuGet dependency:** `AWSSDK.S3`

**S3-specific notes:**
- `SetMetadataAsync` requires a copy-to-self operation because S3 does not support updating metadata in place. The implementation will copy the object to itself with `MetadataDirective.REPLACE` and the new metadata.
- `ExistsAsync` uses `GetObjectMetadataAsync` and catches `AmazonS3Exception` with status code 404 — this is the standard S3 pattern for existence checks.
- `ForcePathStyle` is required for S3-compatible stores (MinIO, LocalStack) that don't support virtual-hosted-style bucket addressing.

---

## Multi-Store Factory & DI Registration

### Consumer Registration

```csharp
services.AddRCommon(builder =>
{
    // Register Azure blob store
    builder.WithBlobStorage<AzureBlobStorageBuilder>(blob =>
    {
        blob.AddBlobStore("primary", options =>
        {
            options.ConnectionString = configuration.GetConnectionString("AzureBlobs");
        });
    });

    // Register S3 blob store (can call WithBlobStorage multiple times)
    builder.WithBlobStorage<AmazonS3ObjectsBuilder>(blob =>
    {
        blob.AddBlobStore("archive", options =>
        {
            options.Region = "us-east-1";
            options.Profile = "production";
        });
    });
});
```

### Consumer Usage

```csharp
public class DocumentService
{
    private readonly IBlobStoreFactory _blobStores;

    public DocumentService(IBlobStoreFactory blobStores)
    {
        _blobStores = blobStores;
    }

    public async Task ArchiveDocumentAsync(string container, string blobName, CancellationToken ct)
    {
        var primary = _blobStores.Resolve("primary");
        var archive = _blobStores.Resolve("archive");

        await using var stream = await primary.DownloadAsync(container, blobName, ct);
        await archive.UploadAsync(container, blobName, stream, token: ct);
        await primary.DeleteAsync(container, blobName, ct);
    }

    public async Task<Uri> GetShareableLinkAsync(string container, string blobName, CancellationToken ct)
    {
        var primary = _blobStores.Resolve("primary");
        return await primary.GetPresignedDownloadUrlAsync(container, blobName, TimeSpan.FromHours(1), ct);
    }
}
```

---

## Projects & Dependencies Summary

| Project | Type | Dependencies | NuGet Packages |
|---------|------|-------------|----------------|
| `RCommon.Blobs` | Abstraction | `RCommon.Core` | — |
| `RCommon.Azure.Blobs` | Implementation | `RCommon.Blobs` | `Azure.Storage.Blobs` |
| `RCommon.Amazon.S3Objects` | Implementation | `RCommon.Blobs` | `AWSSDK.S3` |

**Target frameworks:** `net8.0;net9.0;net10.0`

**Solution folder:** New "Blobs" solution folder containing all three projects.

**csproj conventions** (matching existing projects):
- `<Nullable>enable</Nullable>`
- `<GeneratePackageOnBuild>True</GeneratePackageOnBuild>`
- Apache-2.0 license
- Standard package metadata (Icon, README, Author)
- MinVer for versioning

---

## Testing Strategy

### RCommon.Blobs.Tests (Unit Tests)

1. **BlobStoreFactory tests**
   - `Resolve` returns correct `IBlobStorageService` for registered name
   - `Resolve` throws `BlobStoreNotFoundException` for unregistered name
   - Multiple stores can be registered and resolved independently

2. **Builder extension tests**
   - `WithBlobStorage<T>` registers `IBlobStoreFactory` as singleton
   - `WithBlobStorage<T>` called multiple times does not duplicate factory registration
   - Builder creates instance via `Activator.CreateInstance` with `IRCommonBuilder` parameter

3. **BlobUploadOptions defaults**
   - `Overwrite` defaults to `true`
   - `ContentType` and `Metadata` default to `null`

### RCommon.Azure.Blobs.Tests (Integration Tests)

1. **Full lifecycle against Azurite emulator**
   - Create container → upload blob → download blob → verify content matches
   - List containers, list blobs with prefix
   - Get/set metadata
   - Copy and move blobs
   - Generate presigned download and upload URLs
   - Delete blob → verify exists returns false
   - Delete container

2. **Builder registration tests**
   - `AddBlobStore` with connection string creates valid `BlobServiceClient`
   - `AddBlobStore` with `ServiceUri` + `TokenCredential` creates valid client
   - Missing configuration throws `InvalidOperationException`

### RCommon.Amazon.S3Objects.Tests (Integration Tests)

1. **Full lifecycle against LocalStack**
   - Create container (bucket) → upload → download → verify content matches
   - List buckets, list objects with prefix
   - Get/set metadata (copy-to-self pattern)
   - Copy and move objects
   - Generate presigned download and upload URLs
   - Delete object → verify exists returns false
   - Delete bucket

2. **Builder registration tests**
   - `AddBlobStore` with explicit credentials creates valid `IAmazonS3`
   - `AddBlobStore` with profile creates valid client
   - `AddBlobStore` with default credential chain (no explicit credentials)
   - `ForcePathStyle` flag is respected

---

## File Summary

| File | Action | Location |
|------|--------|----------|
| **RCommon.Blobs (Abstraction)** | | |
| `IBlobStorageService.cs` | Create | `Src/RCommon.Blobs/` |
| `IBlobStorageBuilder.cs` | Create | `Src/RCommon.Blobs/` |
| `BlobStorageBuilderExtensions.cs` | Create | `Src/RCommon.Blobs/` |
| `IBlobStoreFactory.cs` | Create | `Src/RCommon.Blobs/` |
| `BlobStoreFactory.cs` | Create | `Src/RCommon.Blobs/` |
| `BlobStoreFactoryOptions.cs` | Create | `Src/RCommon.Blobs/` |
| `BlobStoreNotFoundException.cs` | Create | `Src/RCommon.Blobs/` |
| `BlobItem.cs` | Create | `Src/RCommon.Blobs/` |
| `BlobProperties.cs` | Create | `Src/RCommon.Blobs/` |
| `BlobUploadOptions.cs` | Create | `Src/RCommon.Blobs/` |
| `RCommon.Blobs.csproj` | Create | `Src/RCommon.Blobs/` |
| **RCommon.Azure.Blobs (Implementation)** | | |
| `IAzureBlobStorageBuilder.cs` | Create | `Src/RCommon.Azure.Blobs/` |
| `AzureBlobStorageBuilder.cs` | Create | `Src/RCommon.Azure.Blobs/` |
| `AzureBlobStorageService.cs` | Create | `Src/RCommon.Azure.Blobs/` |
| `AzureBlobStoreOptions.cs` | Create | `Src/RCommon.Azure.Blobs/` |
| `RCommon.Azure.Blobs.csproj` | Create | `Src/RCommon.Azure.Blobs/` |
| **RCommon.Amazon.S3Objects (Implementation)** | | |
| `IAmazonS3ObjectsBuilder.cs` | Create | `Src/RCommon.Amazon.S3Objects/` |
| `AmazonS3ObjectsBuilder.cs` | Create | `Src/RCommon.Amazon.S3Objects/` |
| `AmazonS3StorageService.cs` | Create | `Src/RCommon.Amazon.S3Objects/` |
| `AmazonS3StoreOptions.cs` | Create | `Src/RCommon.Amazon.S3Objects/` |
| `RCommon.Amazon.S3Objects.csproj` | Create | `Src/RCommon.Amazon.S3Objects/` |
| **Solution** | | |
| `RCommon.sln` | Modify | `Src/` |
| **Tests** | | |
| `RCommon.Blobs.Tests/` | Create | `Tests/` |
| `RCommon.Azure.Blobs.Tests/` | Create | `Tests/` |
| `RCommon.Amazon.S3Objects.Tests/` | Create | `Tests/` |

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Single `IBlobStorageService` interface | Cohesive operations, consistent with RCommon patterns (`ICacheService`), simpler DI |
| `containerName` + `blobName` string parameters | Universal addressing model across Azure and S3, avoids unnecessary abstractions |
| `IBlobStoreFactory` for multi-store | Mirrors `IDataStoreFactory` pattern, enables cross-provider scenarios |
| Factory-delegate pattern (not DI type resolution) | Multiple stores of the same provider (e.g. two Azure stores) can't be disambiguated by DI type; factory delegates keyed by name solve this cleanly |
| `MoveAsync` as copy + delete | Neither Azure nor S3 supports atomic move — documented as non-atomic |
| `SetMetadataAsync` via S3 copy-to-self | S3 limitation — metadata updates require object replacement |
| `Stream` for upload/download | Standard .NET pattern, caller controls lifecycle |
| `BlobUploadOptions.Overwrite` defaults to `true` | Matches common usage; callers opt into conflict detection |
| `WithBlobStorage` callable multiple times | Enables registering multiple providers in one application |
| `TryAddSingleton` for factory registration | Idempotent — safe across multiple `WithBlobStorage` calls |
