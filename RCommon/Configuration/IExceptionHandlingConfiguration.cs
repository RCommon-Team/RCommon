using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Configuration
{
    public interface IExceptionHandlingConfiguration
    {
        void Configure(IContainerAdapter containerAdapter);
    }
}
