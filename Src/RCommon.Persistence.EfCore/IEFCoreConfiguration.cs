using RCommon.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    public interface IEFCoreConfiguration : IRCommonConfiguration
    {
        IEFCoreConfiguration UsingDbContext<TDbContext>() where TDbContext : RCommonDbContext;
    }
}
