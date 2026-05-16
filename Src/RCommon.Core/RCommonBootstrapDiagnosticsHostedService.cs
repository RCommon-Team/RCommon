using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RCommon
{
    /// <summary>
    /// Runs the duplicate-registration scanner once at host startup and emits a single
    /// warning (or stashes the message on the builder) if soft duplicates are detected.
    /// </summary>
    internal sealed class RCommonBootstrapDiagnosticsHostedService : IHostedService
    {
        private readonly IServiceCollection _services;
        private readonly IRCommonBuilder _builder;
        private readonly ILoggerFactory? _loggerFactory;

        public RCommonBootstrapDiagnosticsHostedService(
            IServiceCollection services,
            IRCommonBuilder builder,
            ILoggerFactory? loggerFactory = null)
        {
            _services = services;
            _builder = builder;
            _loggerFactory = loggerFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_builder is not RCommonBuilder rb || !rb.TrySetDiagnosticsRun())
            {
                return Task.CompletedTask;
            }

            var report = _services.GeneratePossibleDuplicatesServiceDescriptorsString();
            if (string.IsNullOrWhiteSpace(report))
            {
                return Task.CompletedTask;
            }

            rb.StashDiagnostics(report);

            if (_loggerFactory is not null)
            {
                var logger = _loggerFactory.CreateLogger<IRCommonBuilder>();
                logger.LogWarning("RCommon bootstrap detected duplicate service registrations:\n{Report}", report);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
