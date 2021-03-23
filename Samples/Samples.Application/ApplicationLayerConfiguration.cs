using RCommon.Configuration;
using RCommon.DependencyInjection;
using Samples.Application.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Samples.Application
{
    /// <summary>
    /// Configuration class for the Application Layer. This allows generic Dependency Injection and general configuration for this specific layer.
    /// </summary>
    /// <remarks>Note that this class is unaware of which DI container it is using which allows us to swap it out at any time
    /// without affecting any of our application code other than what is specific used for container configuration in Startup.</remarks>
    public class ApplicationLayerConfiguration : IServiceConfiguration
    {
        public void Configure(IContainerAdapter containerAdapter)
        {
            // Register all of our application services. 
            containerAdapter.AddTransient<IDiveService, DiveService>();
            containerAdapter.AddTransient<IApplicationUserService, ApplicationUserService>();
        }
    }
}
