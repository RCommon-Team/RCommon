using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ApplicationServices
{
    public class RCommonDomainService<TService> : RCommonService<TService>
    {


        public RCommonDomainService(ILogger<TService> logger, ICommonFactory<IUnitOfWorkScope> unitOfWorkScopeFactory)
            : base(logger)
        {
            UnitOfWorkScopeFactory = unitOfWorkScopeFactory;



        }


        protected ICommonFactory<IUnitOfWorkScope> UnitOfWorkScopeFactory { get; }
    }
}
