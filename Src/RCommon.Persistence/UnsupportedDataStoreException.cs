using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RCommon.Persistence
{
    public class UnsupportedDataStoreException : GeneralException
    {

        public UnsupportedDataStoreException(string message) :base(message)
        {

        }

    }
}
