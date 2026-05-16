using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class SoftDuplicateDiagnosticsTests
{
    [Fact]
    public async Task HostedService_WithSoftDuplicates_EmitsSingleWarning()
    {
        var capturedWarnings = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(capturedWarnings));
        services.AddLogging();

        services.AddRCommon();
        // Inject a duplicate registration to trigger soft-duplicate detection
        services.AddTransient<IFakeService, FakeServiceImpl>();
        services.AddTransient<IFakeService, FakeServiceImpl>();

        var provider = services.BuildServiceProvider();
        foreach (var hs in provider.GetServices<IHostedService>())
        {
            await hs.StartAsync(CancellationToken.None);
        }

        capturedWarnings.Should().HaveCount(1);
        capturedWarnings[0].Should().Contain("FakeServiceImpl");
    }

    [Fact]
    public async Task HostedService_NoSoftDuplicates_EmitsNoWarning()
    {
        var capturedWarnings = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(capturedWarnings));
        services.AddLogging();

        services.AddRCommon();

        var provider = services.BuildServiceProvider();
        foreach (var hs in provider.GetServices<IHostedService>())
        {
            await hs.StartAsync(CancellationToken.None);
        }

        capturedWarnings.Should().BeEmpty();
    }

    [Fact]
    public async Task HostedService_CalledTwice_OnlyRunsScannerOnce()
    {
        var capturedWarnings = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(capturedWarnings));
        services.AddLogging();

        services.AddRCommon();
        services.AddTransient<IFakeService, FakeServiceImpl>();
        services.AddTransient<IFakeService, FakeServiceImpl>();

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var hs in hostedServices)
        {
            await hs.StartAsync(CancellationToken.None);
            await hs.StartAsync(CancellationToken.None);
        }

        capturedWarnings.Should().HaveCount(1);
    }

    public interface IFakeService { }
    public class FakeServiceImpl : IFakeService { }

    private sealed class TestLoggerFactory : ILoggerFactory
    {
        private readonly List<string> _warnings;
        public TestLoggerFactory(List<string> warnings) { _warnings = warnings; }
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => new TestLogger(_warnings);
        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        private readonly List<string> _warnings;
        public TestLogger(List<string> warnings) { _warnings = warnings; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                _warnings.Add(formatter(state, exception));
            }
        }
    }
}
