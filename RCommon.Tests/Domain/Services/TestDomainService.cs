using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.Domain.Repositories;
using RCommon.ExceptionHandling;
using RCommon.ObjectAccess.EFCore.Tests;
using Reactor2.CMS.DomainServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Tests.Domain.Services
{
    public class TestDomainService : CrudDomainService<Customer>, ITestDomainService
    {

        public TestDomainService(IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IEagerFetchingRepository<Customer> repository, ILogger<TestDomainService> logger, IExceptionManager exceptionManager) 
            : base(unitOfWorkScopeFactory, repository, logger, exceptionManager)
        {
            repository.DataStoreName = "TestDbContext";
        }
    }
}
