using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper
{
    public class DapperFluentMappingsException : GeneralException    
    {
        public DapperFluentMappingsException(string message)
            :base(SeverityOptions.Critical, message)
        {

        }
    }
}
