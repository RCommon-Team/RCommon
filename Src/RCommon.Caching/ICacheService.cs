using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    public interface ICacheService
    {
        TData GetOrCreate<TData>(object key, Func<TData> data);
        Task<TData> GetOrCreateAsync<TData>(object key, Func<TData> data);
    }
}
