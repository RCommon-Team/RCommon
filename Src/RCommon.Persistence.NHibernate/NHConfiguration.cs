
using System;
using RCommon.Configuration;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection;
using RCommon.Persistence;
using NHibernate;

namespace RCommon.Persistence.NHibernate
{
    /// <summary>
    /// Implementation of <see cref="IObjectAccessConfiguration"/> that configures RCommon to use NHibernate.
    /// </summary>
    public class NHConfiguration : RCommonConfiguration
    {

        public NHConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

        }
      
        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        public override void Configure()
        {
            this.ContainerAdapter.AddGeneric(typeof(IFullFeaturedRepository<>), typeof(NHRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IReadOnlyRepository<>), typeof(NHRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IWriteOnlyRepository<>), typeof(NHRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IGraphRepository<>), typeof(NHRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(ILinqMapperRepository<>), typeof(NHRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IEagerFetchingRepository<>), typeof(NHRepository<>));
        }


    }
}
