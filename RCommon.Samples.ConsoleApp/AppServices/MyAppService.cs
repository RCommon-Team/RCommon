using AutoMapper;
using Microsoft.Extensions.Logging;
using RCommon.Application.DTO;
using RCommon.Application.Services;
using RCommon.DataServices.Transactions;
using RCommon.Domain.DomainServices;
using RCommon.ExceptionHandling;
using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Samples.ConsoleApp.Domain.Services;
using RCommon.Samples.ConsoleApp.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Samples.ConsoleApp.AppServices
{
    public class MyAppService : CrudAppService<CustomerDto, Customer>, IMyAppService
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IMapper _objectMapper;

        public MyAppService(IOrderService orderService, ICustomerService customerService, IMapper objectMapper, ILogger<MyAppService> logger, IExceptionManager exceptionManager, IUnitOfWorkScopeFactory unitOfWorkScopeFactory)
            : base(customerService, objectMapper, logger, exceptionManager, unitOfWorkScopeFactory)
        {
            this._orderService = orderService;
            this._customerService = customerService;
            this._objectMapper = objectMapper;
        }


        public async Task<CommandResult<bool>> NewCustomerSignupPromotion(CustomerDto customerDto)
        {
            var result = new CommandResult<bool>();
            try
            {
                Guard.Against<NullReferenceException>(_orderService == null, "IOrderService cannot be null");

                using (var scope = this.UnitOfWorkScopeFactory.Create())
                {
                    var customer = this._objectMapper.Map<Customer>(customerDto);
                    var customerCmd = _customerService.Create(customer);

                    if (customerCmd.ValidationResult.IsValid && !customerCmd.HasException)
                    {
                        var item = new OrderItem() { };
                        var order = new Order() { };

                        await _orderService.CreateOrderAsync(order);
                    }
                    else
                    {
                        // There was an issue so let's just send it back to the client
                        result.ValidationResult = customerCmd.ValidationResult;
                    }


                }
            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
                throw ex;
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }

            return result;
        }
    }
}
