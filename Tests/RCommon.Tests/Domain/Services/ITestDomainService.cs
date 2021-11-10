using RCommon.BusinessServices;
using RCommon.Persistence.EFCore.Tests;
using RCommon.TestBase.Entities;
using RCommon.Tests.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Tests.Domain.Services
{
    public interface ITestDomainService : ICrudBusinessService<Customer>
    {
    }
}
