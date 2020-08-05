using RCommon.Application.Services;
using RCommon.ObjectAccess.EFCore.Tests;
using RCommon.Tests.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Tests.Application.Services
{
    public interface ITestAppService : ICrudAppService<CustomerDto>
    {
    }
}
