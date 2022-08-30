using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon
{
	public interface IServiceConfiguration
	{
        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure state storage.
        /// </summary>
        void Configure();

        IContainerAdapter ContainerAdapter { get; }
    }
}
