using Microsoft.Extensions.DependencyInjection;

namespace RCommon.Persistence.Caching.MemoryCache.Tests.TestHelpers;

/// <summary>
/// A test implementation of IPersistenceBuilder for unit testing purposes.
/// </summary>
public class TestPersistenceBuilder : IPersistenceBuilder
{
    public TestPersistenceBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IPersistenceBuilder SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
    {
        Services.Configure(options);
        return this;
    }
}
