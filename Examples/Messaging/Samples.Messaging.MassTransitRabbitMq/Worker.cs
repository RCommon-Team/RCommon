using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Examples.Messaging.MassTransit
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Example Starting");
                var eventProducers = _serviceProvider.GetServices<IEventProducer>();
                var testEvent = new TestEvent(DateTime.Now, Guid.NewGuid());

                foreach (var producer in eventProducers)
                {
                    Console.WriteLine($"Producer: {producer}");
                    await producer.ProduceEventAsync(testEvent);
                }
                await Task.Delay(5000, stoppingToken);
                
            }
        }
    }
}
