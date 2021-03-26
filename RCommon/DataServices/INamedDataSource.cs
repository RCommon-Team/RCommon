using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public interface INamedDataSource
    {
        public string DataStoreName { get; set; }
    }
}
