using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.BusinessServices
{
    public abstract class RCommonBusinessService : RCommonService
    {


        public RCommonBusinessService(ILogger logger, IExceptionManager exceptionManager)
            : base(logger)
        {
            ExceptionManager = exceptionManager;
        }

        public IExceptionManager ExceptionManager { get; }

    }
}
