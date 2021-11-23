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

        public DistributingAppService(ILogger logger, IExceptionManager exceptionManager, IUnitOfWorkScopeFactory unitOfWorkScopeFactory
            , IDistributedEventBroker distributedEventBroker) 
            : base(logger, exceptionManager, unitOfWorkScopeFactory)
        {
            DistributedEventBroker = distributedEventBroker;
        }

        public IDistributedEventBroker DistributedEventBroker { get; }
    }
}
