using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class DataStoreNotFoundException : GeneralException
    {
        public DataStoreNotFoundException(string message) : base(message)
        {

        }
    }
}
