using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;

namespace RCommon.TestBase
{
    public static class SerilogBootstrapper
    {
        public static LoggerConfiguration BuildLoggerConfig(IConfiguration configuration)
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            var logConfig = new LoggerConfiguration()
                .WriteTo.Console()
                .ReadFrom.Configuration(configuration)
                // Just adding Enrichers in code to keep config cleaner, we will always want them
                .Enrich.FromLogContext();

            return logConfig;
        }
    }
}
