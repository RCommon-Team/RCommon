using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public interface IEFCoreConfiguration : IPersistenceConfiguration
    {
        IEFCoreConfiguration AddDbContext<TDbContext>(string dataStoreName, Action<DbContextOptionsBuilder>? options) where TDbContext : RCommonDbContext;
    }
}
