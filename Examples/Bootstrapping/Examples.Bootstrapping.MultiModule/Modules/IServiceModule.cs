using Microsoft.Extensions.DependencyInjection;

namespace Examples.Bootstrapping.MultiModule.Modules;

/// <summary>
/// Minimal contract a module implements to configure services in a multi-module composition.
/// Each module independently calls <c>services.AddRCommon()</c>; the bootstrapper guarantees
/// idempotent and merge-able registration semantics across modules.
/// </summary>
public interface IServiceModule
{
    void Configure(IServiceCollection services);
}
