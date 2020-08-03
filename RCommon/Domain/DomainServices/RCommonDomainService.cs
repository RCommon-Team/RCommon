using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Domain.DomainServices
{
    public abstract class RCommonDomainService : RCommonService
    {


        public RCommonDomainService(ILogger logger, IExceptionManager exceptionManager)
            : base(logger)
        {
            ExceptionManager = exceptionManager;
        }

        public IExceptionManager ExceptionManager { get; }

    }
}
