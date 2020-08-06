using RCommon.Application.DTO;
using RCommon.Domain.DomainServices;
using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Samples.ConsoleApp.Domain.Services
{
    public interface ICustomerService : ICrudDomainService<Customer>
    {
        Task<CommandResult<Customer>> GetFirstCustomer(string lastName);
    }
}
