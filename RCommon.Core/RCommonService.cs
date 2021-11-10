using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public abstract class RCommonService
    {

        public RCommonService(ILogger logger)
        {
            Logger = logger;



        }

        public ILogger Logger { get; }


    }
}
