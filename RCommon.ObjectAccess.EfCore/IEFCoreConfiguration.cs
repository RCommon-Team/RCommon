using RCommon.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore
{
    public interface IEFCoreConfiguration : IServiceConfiguration
    {
        IEFCoreConfiguration UsingDbContext<TDbContext>() where TDbContext : RCommonDbContext;
    }
}
