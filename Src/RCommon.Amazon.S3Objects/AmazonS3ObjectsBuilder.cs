using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Blobs;

namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Concrete builder that registers Amazon S3 services into the DI container.
/// Constructor accepts <see cref="IRCommonBuilder"/> (required by Activator.CreateInstance pattern).
/// </summary>
public class AmazonS3ObjectsBuilder : IAmazonS3ObjectsBuilder
{
    public IServiceCollection Services { get; }

    public AmazonS3ObjectsBuilder(IRCommonBuilder builder)
    {
        Services = builder.Services;
    }

    /// <inheritdoc />
    public IAmazonS3ObjectsBuilder AddBlobStore(string name, Action<AmazonS3StoreOptions> options)
    {
        var storeOptions = new AmazonS3StoreOptions();
        options(storeOptions);

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
                else if (storeOptions.Profile != null)
                {
                    var chain = new CredentialProfileStoreChain();
                    if (!chain.TryGetAWSCredentials(storeOptions.Profile, out var credentials))
                    {
                        throw new InvalidOperationException(
                            $"AWS profile '{storeOptions.Profile}' not found.");
                    }
                    client = new AmazonS3Client(credentials, config);
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
