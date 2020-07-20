
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace NTeris.Core.Web.AspNetCore.Configuration
{
    public class AspNetCoreConfiguration : IRCommonConfiguration
    {
        public void Configure(IContainerAdapter containerAdapter)
        {
            containerAdapter.Register<IVirtualPathUtilityService, VirtualPathUtilityService>();
            //containerAdapter.Register<ILocalState, HttpLocalState>();
            //containerAdapter.Register<ICacheState, HttpRuntimeCache>();
            //containerAdapter.Register<ISessionState, HttpSessionState>();
        }
    }
}
