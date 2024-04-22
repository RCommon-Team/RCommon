using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging
{
    public static class ILoggerExtensions
    {

        public static void LogInformation(this ILogger logger, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(message, @params);

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        public static void LogInformation(this ILogger logger, EventId eventId, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(eventId, message, @params);

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        public static void LogInformation(this ILogger logger, Exception exception, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(exception, message, @params);

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        public static void LogInformation(this ILogger logger, EventId eventId, Exception exception, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(eventId, exception, message, @params);

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        private static void OutputToConsole(string? message, object[]? @params)
        {
            System.Console.WriteLine(message, @params);
        }
    }
}
