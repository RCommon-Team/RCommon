using Microsoft.Extensions.DependencyInjection;

namespace RCommon.Blobs;

/// <summary>
/// Base builder interface for configuring blob storage providers within the RCommon builder pipeline.
/// </summary>
public interface IBlobStorageBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to register blob storage dependencies.
    /// </summary>
    IServiceCollection Services { get; }
}
