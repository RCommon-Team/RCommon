using RCommon.Configuration;
using RCommon.Persistence.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public interface IEFCoreConfiguration : IObjectAccessConfiguration
    {
        IEFCoreConfiguration UsingDbContext<TDbContext>() where TDbContext : RCommonDbContext;
    }
}
