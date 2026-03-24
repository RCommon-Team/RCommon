using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RCommon.Blobs;

/// <summary>
/// Extension methods on <see cref="IRCommonBuilder"/> for registering blob storage infrastructure.
/// </summary>
public static class BlobStorageBuilderExtensions
{
    /// <summary>
    /// Registers a blob storage provider with default configuration.
    /// </summary>
    public static IRCommonBuilder WithBlobStorage<T>(this IRCommonBuilder builder)
        where T : IBlobStorageBuilder
    {
        return WithBlobStorage<T>(builder, x => { });
    }

    /// <summary>
    /// Registers a blob storage provider and applies the specified configuration actions.
    /// Can be called multiple times to register multiple providers (e.g. Azure + S3).
    /// </summary>
    public static IRCommonBuilder WithBlobStorage<T>(this IRCommonBuilder builder, Action<T> actions)
        where T : IBlobStorageBuilder
    {
        Guard.IsNotNull(actions, nameof(actions));

        builder.Services.TryAddSingleton<IBlobStoreFactory, BlobStoreFactory>();

        var blobConfig = (T)(Activator.CreateInstance(typeof(T), new object[] { builder })
            ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}."));
        actions(blobConfig);
        return builder;
    }
}
