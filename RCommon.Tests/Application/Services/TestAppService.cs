using AutoMapper;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using RCommon.ObjectAccess.EFCore.Tests;
using RCommon.Tests.Application.DTO;
using RCommon.Tests.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Tests.Application.Services
{
    public class TestAppService : CrudAppService<CustomerDto, Customer>, ITestAppService
    {

        public TestAppService(ITestDomainService domainService, IMapper objectMapper, ILogger<TestAppService> logger, IExceptionManager exceptionManager, IUnitOfWorkScopeFactory unitOfWorkScopeFactory) 
            : base(domainService, objectMapper, logger, exceptionManager, unitOfWorkScopeFactory)
        {

        }

      
    }
}
