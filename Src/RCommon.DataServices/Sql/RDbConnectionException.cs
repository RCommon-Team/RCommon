using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices.Sql
{
    public class RDbConnectionException : GeneralException
    {

        public RDbConnectionException(string message) 
            : base(message)
        {

        }
    }
}
