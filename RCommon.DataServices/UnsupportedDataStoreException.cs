using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RCommon.DataServices
{
    public class UnsupportedDataStoreException : ApplicationException
    {

        public UnsupportedDataStoreException(string message) :base(message)
        {

        }

    }
}
