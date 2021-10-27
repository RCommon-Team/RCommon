using RCommon.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistance.EFCore
{
    public interface IEFCoreConfiguration : IServiceConfiguration
    {
        IEFCoreConfiguration UsingDbContext<TDbContext>() where TDbContext : RCommonDbContext;
    }
}
