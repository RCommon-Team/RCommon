using Microsoft.Extensions.Logging;
using RCommon.Application.DTO;
using RCommon.DataServices.Transactions;
using RCommon.Domain.DomainServices;
using RCommon.Domain.Repositories;
using RCommon.ExceptionHandling;
using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.Domain.BusinessRules;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Samples.ConsoleApp.Domain.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Samples.ConsoleApp.Domain.Services
{
    public class CustomerService : CrudDomainService<Customer>, ICustomerService
    {
        private readonly IEagerFetchingRepository<Customer> _repository;

        public CustomerService(IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IEagerFetchingRepository<Customer> repository, ILogger<CustomerService> logger, IExceptionManager exceptionManager) 
            : base(unitOfWorkScopeFactory, repository, logger, exceptionManager)
        {
            repository.DataStoreName = "TestDbContext";
            this._repository = repository;

            this.AddRulesAndValidators();
        }

        private void AddRulesAndValidators()
        {
            this.SetEntityValidator(new CustomerValidator()); // This will get called before execution against repository

            this.SetBusinessRulesEvaluator(new CustomerBusinessRulesEvaluator()); // These rules will be evaluated before execution against repository
        }


        /// <summary>
        /// An example of how to extend a domain service that implements <see cref="CrudDomainService{TEntity}"/>
        /// </summary>
        /// <param name="lastName"></param>
        /// <returns><see cref="CommandResult{TResult}"/> with populated Customer in the DataResult property</returns>
        public async Task<CommandResult<Customer>> GetFirstCustomer(string lastName)
        {
            var cmd = new CommandResult<Customer>();
            try
            {
                
                var customer = await _repository.FindSingleOrDefaultAsync(x => x.LastName == lastName);
                cmd.DataResult = customer;
            }
            catch (Exception ex)
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
                throw;
            }

            return cmd;
        }
    }
}
