using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public class CommonBusinessEntitiesConfiguration : RCommonConfiguration
    {
        public CommonBusinessEntitiesConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

            this.ContainerAdapter.AddTransient<IChangeTracker, ChangeTracker>();
        }


    }
}
