using Examples.Validation.FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.ExecutionResults;
using RCommon.FluentValidation;
using System.Diagnostics;

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
                        .WithValidation<FluentValidationBuilder>(validation =>
                        {
                            validation.AddValidatorsFromAssemblyContaining(typeof(TestDto));
                        });
                    Console.WriteLine(services.GenerateServiceDescriptorsString());
                    services.AddTransient<ITestApplicationService, TestApplicationService>();

                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();
    var validationOutcome1 = await appService.ExecuteTestMethod(new TestDto("")); // Will fail
    var validationOutcome2 = await appService.ExecuteTestMethod(new TestDto("test")); // Will pass

    Console.WriteLine(validationOutcome1.ToString());
    Console.WriteLine("Validation Outcome 1 complete...");
    Console.WriteLine(validationOutcome2.ToString());
    Console.WriteLine("Validation Outcome 2 complete...");

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

