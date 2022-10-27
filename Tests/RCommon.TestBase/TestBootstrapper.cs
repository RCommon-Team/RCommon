
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Serilog;

namespace RCommon.TestBase
{
    public abstract class TestBootstrapper
    {
        private ServiceProvider _serviceProvider;
        private Microsoft.Extensions.Logging.ILogger _logger;

        static object _configureLock = new object();

        public TestBootstrapper()
        {

        }

        protected virtual void InitializeBootstrapper(IServiceCollection services)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            this.Configuration = builder.Build();
            services.AddSingleton<IConfiguration>(this.Configuration);
            services.AddLogging(x => x.AddSerilog(SerilogBootstrapper.BuildLoggerConfig(this.Configuration).CreateLogger(), dispose: true));
        }



        /// <summary>
        /// This simplifies the process of mocking web requests and testing responses over Http
        /// </summary>
        /// <param name="mockResponse"></param>
        /// <returns>Mock HttpClient Factory</returns>
        /// <remarks>Usage: var mockFactory = this.CreateMockHttpClient(new HttpResponseMessage{StatusCode = HttpStatusCode.InternalServerError};
        ///                 MyController controller = new MyController(mockFactory.Object);
        ///                 var result = await controller.GetAllUsers();
        ///                 Assert.Null(result);
        ///</remarks>
        protected Mock<IHttpClientFactory> CreateMockHttpClient(HttpResponseMessage mockResponse)
        {
            // we create a mock of IHttpClientFactory
            var mockFactory = new Mock<IHttpClientFactory>();

            // The MessageHandler is a class that receives an HTTP request and returns an HTTP response.
            // For this reason, we create a mock of it where we define
            // the status code and the result
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            var client = new HttpClient(mockHttpMessageHandler.Object);
            // here we define the IHttpClientFactory's CreateClient method 
            mockFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            return mockFactory;
        }

        public IConfigurationRoot Configuration { get; private set; }
        public ServiceProvider ServiceProvider { get => _serviceProvider; set => _serviceProvider = value; }
        public Microsoft.Extensions.Logging.ILogger Logger { get => _logger; set => _logger = value; }
    }
}
