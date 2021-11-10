
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
using System.Transactions;

namespace RCommon.TestBase
{
    public abstract class TestBootstrapper
    {
        private ServiceProvider _serviceProvider;
        private ILogger _logger;

        static object _configureLock = new object();

        public TestBootstrapper()
        {

        }

        protected virtual void InitializeBootstrapper(IServiceCollection services)
        {



            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            this.Configuration = config.Build();

            services.AddSingleton<ILogger>(TestLogger.Create());
            services.AddSingleton<IConfiguration>(this.Configuration);
            services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));
        }



        /// <summary>
        /// Creates a simple web request so that we can test RCommon in web environment
        /// </summary>
        protected void CreateWebRequest()
        {
            string response = "my test response";
            TestWebRequest.RegisterPrefix("test", new TestWebRequestCreate());
            TestWebRequest request = TestWebRequestCreate.CreateTestRequest(response);


            // DoStuff call the url with a request and then processes the 
            // response as set above myObject.DoStuff(); 
            string requestContent = request.ContentAsString();
            //Assert.AreEqual(expectedRequestContent, requestContent);
        }

        public IConfigurationRoot Configuration { get; private set; }
        public ServiceProvider ServiceProvider { get => _serviceProvider; set => _serviceProvider = value; }
        public ILogger Logger { get => _logger; set => _logger = value; }
    }
}
