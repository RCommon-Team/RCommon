

using CommonServiceLocator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.Configuration;
using RCommon.DataServices;
using RCommon.DependencyInjection;
using RCommon.DependencyInjection.Microsoft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging.Console;
using System.Transactions;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    public abstract class TestBase
    {
        private ServiceProvider _serviceProvider;
        private ILogger _logger;

        static object _configureLock = new object();
        
        public TestBase()
        {

        }

        protected void InitializeRCommon(IServiceCollection services)
        {
           


            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            this.Configuration = config.Build();

            if (_serviceProvider == null)
            {
                
                services.AddSingleton<ILogger>(TestLogger.Create());
                services.AddSingleton<IConfiguration>(this.Configuration);
                services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));
                //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

                /*ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services)) // By default we'll be using Theadlocal storage since we're not under web request
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .WithUnitOfWork<DefaultUnitOfWorkConfiguration>()
                .WithObjectAccess<EFCoreConfiguration>();*/

                services.AddDbContext<RCommonDbContext, TestDbContext>();

                ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .WithUnitOfWork<DefaultUnitOfWorkConfiguration>()
                .WithObjectAccess<EFCoreConfiguration>(
                    x => x.UsingDbContext<TestDbContext>());
                /*.WithExceptionHandling<DefaultExceptionHandlingConfiguration>(exHandling =>
                {
                    exHandling.WithExceptionPolicy<CatchAllExceptionPolicy>("BasePolicy", policy => // This should be our catchall policy
                    {

                        policy.EnlistPublisher<LoggingPublisher>(publisher =>
                        {
                            publisher.UseFormatter<ExceptionToTextFormatter>();
                        });
                        policy.EnlistException<Exception>();
                        policy.UseHandler<ExceptionRethrowHandler>();

                    });
                    //var businessWrapPolicy = x.WithExceptionPolicy<BusinessExceptionWrapPolicy>("BusinessWrapPolicy");
                    
                });*/

                //services.AddDbContext<RCommonDbContext, TestDbContext>(ServiceLifetime.Transient);

                _serviceProvider = services.BuildServiceProvider();

                Debug.WriteLine($"Total Services Registered: {services.Count}");
                foreach (var service in services)
                {
                    Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
                }
                _logger = this.ServiceProvider.GetService<ILogger>();
            }

            
        }



        /// <summary>
        /// Creates a simple web request so that we can test RCommon in web environment
        /// </summary>
        protected void CreateWebRequest()
        {
            string response = "my test response"; 
            TestWebRequest.RegisterPrefix("test", new TestWebRequestCreate()); 
            TestWebRequest request = TestWebRequestCreate.CreateTestRequest(response); 
#pragma warning disable CS0219 // The variable 'url' is assigned but its value is never used
            string url = "http://localhost://test"; 
#pragma warning restore CS0219 // The variable 'url' is assigned but its value is never used
            //ObjectUnderTest myObject = new ObjectUnderTest(); 
            //myObject.Url = url; 
            
            // DoStuff call the url with a request and then processes the 
            // response as set above myObject.DoStuff(); 
            string requestContent = request.ContentAsString(); 
            //Assert.AreEqual(expectedRequestContent, requestContent);
        }

        public IConfigurationRoot Configuration { get; private set; }
        public ServiceProvider ServiceProvider { get => _serviceProvider;  }
        public ILogger Logger { get => _logger; set => _logger = value; }
    }
}
