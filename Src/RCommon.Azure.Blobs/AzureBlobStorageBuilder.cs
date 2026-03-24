using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Blobs;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Concrete builder that registers Azure Blob Storage services into the DI container.
/// Constructor accepts <see cref="IRCommonBuilder"/> (required by Activator.CreateInstance pattern).
/// </summary>
public class AzureBlobStorageBuilder : IAzureBlobStorageBuilder
{
    public IServiceCollection Services { get; }

    public AzureBlobStorageBuilder(IRCommonBuilder builder)
    {
        Services = builder.Services;
    }

    /// <inheritdoc />
    public IAzureBlobStorageBuilder AddBlobStore(string name, Action<AzureBlobStoreOptions> options)
    {
        var storeOptions = new AzureBlobStoreOptions();
        options(storeOptions);

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
