using Microsoft.Extensions.Logging;
using RCommon.Domain.DomainServices;
using RCommon.ExceptionHandling;
using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Samples.ConsoleApp.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Samples.ConsoleApp.Domain.Services
{
    public class OrderService : RCommonDomainService, IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(ILogger<OrderService> logger, IExceptionManager exceptionManager, IOrderRepository orderRepository) : base(logger, exceptionManager)
        {
            this._orderRepository = orderRepository;
        }

        public async Task CreateOrderAsync(Order order)
        {
            try
            {
                await _orderRepository.AddAsync(order);
                this.Logger.LogInformation("Created order of type {0}", order.GetType().Name);
            }
            catch (Exception ex)
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
                throw ex;
            }
        }
    }
}
