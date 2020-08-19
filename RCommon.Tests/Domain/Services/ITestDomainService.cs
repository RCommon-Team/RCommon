using RCommon.Application.Services;
using RCommon.Domain.DomainServices;
using RCommon.ObjectAccess.EFCore.Tests;
using RCommon.Tests.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Tests.Domain.Services
{
    public interface ITestDomainService : ICrudDomainService<Customer>
    {
    }
}
