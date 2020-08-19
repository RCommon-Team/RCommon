using RCommon.Configuration;
using RCommon.DependencyInjection;
using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.AppServices;
using RCommon.Samples.ConsoleApp.Domain.Repositories;
using RCommon.Samples.ConsoleApp.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Samples.ConsoleApp
{
    public class SampleAppConfiguration : IServiceConfiguration
    {
        public void Configure(IContainerAdapter containerAdapter)
        {
            // Repositories
            containerAdapter.AddGeneric(typeof(IEncapsulatedRepository<>), typeof(EncapsulatedRepository<>));
            containerAdapter.AddTransient<IOrderRepository, OrderRepository>();

            // Domain Services
            containerAdapter.AddTransient<ICustomerService, CustomerService>();
            containerAdapter.AddTransient<IOrderService, OrderService>();

            // Application Services
            containerAdapter.AddTransient<IMyAppService, MyAppService>();
        }
    }
}
