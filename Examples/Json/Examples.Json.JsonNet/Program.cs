using Examples.Json.JsonNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Json;
using RCommon.JsonNet;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;

try
{
    var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {

                    ConfigurationContainer.Configuration = builder
                        .Build();
                })
                .ConfigureServices(services =>
                {
                    // Configure RCommon
                    services.AddRCommon()
                        .WithJsonSerialization<JsonNetBuilder>(json =>
                        {
                            
                            json.CamelCase = true;
                            json.Indented = true;
                        });
                    
                    services.AddTransient<ITestApplicationService, TestApplicationService>();
                    
                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();
    string json = appService.Serialize(new TestDto("This is my "));
    TestDto dto = appService.Deserialize("{ // TestDto\r\n  \"message\": This is my deserialized message\r\n}");

    Console.WriteLine(json);
    Console.WriteLine(dto.Message);

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

