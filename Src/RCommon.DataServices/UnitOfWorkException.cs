using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public class UnitOfWorkException : GeneralException
    {
        public UnitOfWorkException(string message) : base(message)
        {

        }
    }
}
