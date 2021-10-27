
using System;
using RCommon.Configuration;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection;
using RCommon.Persistance;
using NHibernate;

namespace RCommon.Persistance.NHibernate
{
    /// <summary>
    /// Implementation of <see cref="IObjectAccessConfiguration"/> that configures RCommon to use NHibernate.
    /// </summary>
    public class NHConfiguration : IServiceConfiguration
    {

        
      
        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance that allows
        /// registering components.</param>
        public void Configure(IContainerAdapter containerAdapter)
        {
            containerAdapter.AddGeneric(typeof(IFullFeaturedRepository<>), typeof(NHRepository<>));
        }


    }
}