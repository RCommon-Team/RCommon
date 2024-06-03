using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class PersistenceException : GeneralException
    {
        public PersistenceException(string message, Exception exception) : base(message, exception)
        {
            
        }
    }
}
