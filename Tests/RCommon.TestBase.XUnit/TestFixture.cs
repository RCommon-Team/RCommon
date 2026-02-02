using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RCommon.TestBase.XUnit;

/// <summary>
/// Base test fixture providing common services and configuration for XUnit tests.
/// </summary>
public abstract class TestFixture : IDisposable
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IServiceCollection Services { get; }
    protected IConfiguration Configuration { get; private set; }

    protected TestFixture()
    {
        Services = new ServiceCollection();
        Configuration = BuildConfiguration();
        ConfigureServices(Services);
        ServiceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Builds the configuration from appsettings.json if present.
    /// </summary>
    protected virtual IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        return builder.Build();
    }

    /// <summary>
    /// Override to configure services for dependency injection in tests.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(Configuration);
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }

    /// <summary>
    /// Gets a service from the service provider.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets an optional service from the service provider.
    /// </summary>
    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            (ServiceProvider as IDisposable)?.Dispose();
        }
    }
}
