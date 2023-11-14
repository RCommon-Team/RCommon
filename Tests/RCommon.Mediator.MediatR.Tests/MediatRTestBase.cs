using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Tests
{
    public class MediatRTestBase : TestBootstrapper
    {

        public MediatRTestBase() : base()
        {
                
        }

        public TestServer Server { get; private set; }
        public HttpClient Client { get; private set; }

        protected void InitializeRCommon(IServiceCollection services)
        {
            var builder = WebHost.CreateDefaultBuilder()
                .UseEnvironment(EnvironmentName.Development)
                .ConfigureTestServices(
                    services =>
                    {
                        services.AddTransient((a) => this.SomeMockService.Object);
                        services.AddRCommon()
                        .WithSequentialGuidGenerator(guidOptions =>
                        {
                            guidOptions.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString;
                        });

                        this.ServiceProvider = services.BuildServiceProvider();
                        this.Logger = this.ServiceProvider.GetService<ILogger<MediatRTestBase>>();

                        Debug.WriteLine($"Total Services Registered: {services.Count}");
                        foreach (var service in services)
                        {
                            Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
                        }
                    });

            this.Server = new TestServer(builder);
            this.Client = this.Server.CreateClient();
            this.Client.BaseAddress = new Uri("http://localhost");

            base.InitializeBootstrapper(services);

            
        }

    }

}
