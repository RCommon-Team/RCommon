using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public class RCommonService<TService>
    {

        public RCommonService(ILogger<TService> logger)
        {
            Logger = logger;



        }

        public ILogger<TService> Logger { get; }


    }
}
