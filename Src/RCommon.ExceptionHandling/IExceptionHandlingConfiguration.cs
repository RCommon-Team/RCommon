using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ExceptionHandling
{
    public interface IExceptionHandlingConfiguration : IServiceConfiguration
    {

        IExceptionHandlingConfiguration UsingDefaultExceptionPolicies();
    }
}
