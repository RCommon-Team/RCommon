using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.Configuration
{
	public interface IServiceConfiguration
	{
        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure state storage.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance that can be
        /// used to register state storage components.</param>
        void Configure(IContainerAdapter containerAdapter);
    }
}
