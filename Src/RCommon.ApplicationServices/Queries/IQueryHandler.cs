using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Queries
{
    public interface IQueryHandler<in TQuery, TResult>
        where TQuery : class, IQuery<TResult>
    {
    }
}
