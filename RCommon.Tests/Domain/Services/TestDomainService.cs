using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using RCommon.BusinessServices;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using RCommon.ObjectAccess;
using RCommon.ObjectAccess.EFCore.Tests;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Tests.Domain.Services
{
    public class TestDomainService : CrudBusinessService<Customer>, ITestDomainService
    {

        public TestDomainService(IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IFullFeaturedRepository<Customer> repository, ILogger<TestDomainService> logger, IExceptionManager exceptionManager) 
            : base(unitOfWorkScopeFactory, repository, logger, exceptionManager)
        {
            repository.DataStoreName = "TestDbContext";
        }
    }
}
