using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public class InvalidCacheException : GeneralException
    {
        public InvalidCacheException(string message):base(message)
        {
            
        }
    }
}
