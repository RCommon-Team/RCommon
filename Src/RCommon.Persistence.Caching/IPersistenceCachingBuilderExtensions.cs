using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching
{
    public static class IPersistenceCachingBuilderExtensions
    {
        public static IPersistenceCachingBuilder Configure(this IPersistenceCachingBuilder builder)
        {
            
            return builder;
        }
    }
}
