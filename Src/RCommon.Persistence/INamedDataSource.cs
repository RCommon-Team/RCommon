using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface INamedDataSource
    {
        public string DataStoreName { get; set; }
    }
}
