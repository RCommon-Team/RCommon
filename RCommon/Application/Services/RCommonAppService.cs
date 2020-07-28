using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Application.Services
{
    public class RCommonAppService<TService> : RCommonService<TService>
    {


        public RCommonAppService(ILogger<TService> logger, IExceptionManager exceptionManager, IUnitOfWorkScopeFactory<IUnitOfWorkScope> unitOfWorkScopeFactory)
            : base(logger)
        {
            ExceptionManager = exceptionManager;
            UnitOfWorkScopeFactory = unitOfWorkScopeFactory;



        }

        public IExceptionManager ExceptionManager { get; }

        protected IUnitOfWorkScopeFactory<IUnitOfWorkScope> UnitOfWorkScopeFactory { get; }
    }
}
