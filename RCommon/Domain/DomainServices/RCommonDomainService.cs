using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Domain.DomainServices
{
    public abstract class RCommonDomainService<TService> : RCommonService<TService>
    {


        public RCommonDomainService(ILogger<TService> logger, IExceptionManager exceptionManager)
            : base(logger)
        {
            ExceptionManager = exceptionManager;
        }

        public IExceptionManager ExceptionManager { get; }

    }
}
