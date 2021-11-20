using MassTransit;
using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging
{
    public class DistributingAppService : RCommonAppService
    {

        public DistributingAppService(ILogger logger, IExceptionManager exceptionManager, IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IPublishEndpoint endpoint) 
            : base(logger, exceptionManager, unitOfWorkScopeFactory)
        {
            DistributionEndpoint = endpoint;
        }

        public IPublishEndpoint DistributionEndpoint { get; }
    }
}
