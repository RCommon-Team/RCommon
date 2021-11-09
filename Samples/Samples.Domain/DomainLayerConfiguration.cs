using RCommon.Configuration;
using RCommon.DependencyInjection;
using Samples.Domain.DomainServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Samples.Domain
{
    /// <summary>
    /// Configuration class for the Domain Layer. This allows generic Dependency Injection and general configuration for this specific layer.
    /// </summary>
    /// <remarks>Note that this class is unaware of which DI container it is using which allows us to swap it out at any time
    /// without affecting any of our application code other than what is specific used for container configuration in Startup.</remarks>
    public class DomainLayerConfiguration : RCommonConfiguration, IServiceConfiguration
    {
        public DomainLayerConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

        }


        public override void Configure()
        {
            // Register all of the domain services with the Dependency Injection Container. 
            this.ContainerAdapter.AddTransient<IDiveLocationService, DiveLocationService>();
            this.ContainerAdapter.AddTransient<IDiveTypeService, DiveTypeService>();
            this.ContainerAdapter.AddTransient<IUserService, UserService>();
        }
    }
}
