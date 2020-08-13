using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.Configuration;
using RCommon.DependencyInjection.Microsoft;
using RCommon.ExceptionHandling.EnterpriseLibraryCore;
using RCommon.ObjectAccess.EFCore;
using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.AppServices;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Samples.ConsoleApp.Shared.Dto;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RCommon.Samples.ConsoleApp
{

    class Program
    {

        private static IServiceProvider _serviceProvider;
        private static IConfiguration Configuration;


        static void Main(string[] args)
        {

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            Configuration = config.Build();

            var services = new ServiceCollection();

            services.AddSingleton<ILogger>(TestLogger.Create());
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));


            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .WithUnitOfWork<DefaultUnitOfWorkConfiguration>()
                .WithObjectAccess<EFCoreConfiguration>(x =>
                {
                    // Add all the DbContexts here
                    x.UsingDbContext<TestDbContext>();
                })
                .WithExceptionHandling<EhabExceptionHandlingConfiguration>(x =>
                    x.UsingDefaultExceptionPolicies())
                .And<CommonApplicationServicesConfiguration>()
                .And<SampleAppConfiguration>();

            // Mapping Profiles
            services.AddAutoMapper(x =>
            {
                x.AddProfile<MappingProfile>();
            });

            _serviceProvider = services.BuildServiceProvider();

            // Add Local Services

            Start();

            Console.ReadLine();
        }

        private static void Start()
        {
            var appService = _serviceProvider.GetService<IMyAppService>();


            // This should work fine
            var newCustomer = new CustomerDto()
            {
                City = "Hollywood",
                FirstName = "Steve",
                LastName = "Smith",
                State = "Florida",
                StreetAddress1 = "123 Test Way",
                ZipCode = "93847"
            };

            var cmd = appService.NewCustomerSignupPromotion(newCustomer);

            if (cmd.Result.DataResult)
            {
                Console.WriteLine("New Customer sign up completed successfully!");
            }
            else
            {
                Console.WriteLine("New Customer sign up failed...");
            }

            // This should fail without an exception because we don't want people from zipcode 30062
            var newCustomer2 = new CustomerDto()
            {
                City = "Hollywood",
                FirstName = "Steve",
                LastName = "Smith",
                State = "Florida",
                StreetAddress1 = "123 Test Way",
                ZipCode = "30062"
            };

            var cmd2 = appService.NewCustomerSignupPromotion(newCustomer2);

            if (cmd2.Result.DataResult)
            {
                Console.WriteLine("New Customer sign up completed successfully!");
            }
            else
            {
                Console.WriteLine("New Customer sign up failed...");
            }
        }
    }
}
