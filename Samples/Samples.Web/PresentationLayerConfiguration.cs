using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Web
{
    public class PresentationLayerConfiguration : RCommonConfiguration
    {
        public PresentationLayerConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

        }
    }
}
