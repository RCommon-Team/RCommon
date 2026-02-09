using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides extension methods for <see cref="ILogger"/> that add optional console output alongside standard logging.
    /// </summary>
    public static class ILoggerExtensions
    {

        /// <summary>
        /// Logs an informational message with optional console output.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The log message template.</param>
        /// <param name="params">The message format parameters.</param>
        /// <param name="outputToConsole">If <c>true</c>, also writes the message to the console.</param>
        public static void LogInformation(this ILogger logger, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(message, @params ?? System.Array.Empty<object>());

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        /// <summary>
        /// Logs an informational message with an event ID and optional console output.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="eventId">The event ID associated with the log entry.</param>
        /// <param name="message">The log message template.</param>
        /// <param name="params">The message format parameters.</param>
        /// <param name="outputToConsole">If <c>true</c>, also writes the message to the console.</param>
        public static void LogInformation(this ILogger logger, EventId eventId, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(eventId, message, @params ?? System.Array.Empty<object>());

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        /// <summary>
        /// Logs an informational message with an exception and optional console output.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="exception">The exception associated with the log entry.</param>
        /// <param name="message">The log message template.</param>
        /// <param name="params">The message format parameters.</param>
        /// <param name="outputToConsole">If <c>true</c>, also writes the message to the console.</param>
        public static void LogInformation(this ILogger logger, Exception exception, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(exception, message, @params ?? System.Array.Empty<object>());

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        /// <summary>
        /// Logs an informational message with an event ID, exception, and optional console output.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="eventId">The event ID associated with the log entry.</param>
        /// <param name="exception">The exception associated with the log entry.</param>
        /// <param name="message">The log message template.</param>
        /// <param name="params">The message format parameters.</param>
        /// <param name="outputToConsole">If <c>true</c>, also writes the message to the console.</param>
        public static void LogInformation(this ILogger logger, EventId eventId, Exception exception, string? message, object[]? @params, bool outputToConsole = false)
        {
            logger.LogInformation(eventId, exception, message, @params ?? System.Array.Empty<object>());

            if (outputToConsole)
            {
                OutputToConsole(message, @params);
            }
        }

        /// <summary>
        /// Writes a formatted message to the console using <see cref="System.Console.WriteLine(string, object[])"/>.
        /// </summary>
        /// <param name="message">The message template.</param>
        /// <param name="params">The format parameters.</param>
        private static void OutputToConsole(string? message, object[]? @params)
        {
            System.Console.WriteLine(message ?? string.Empty, @params ?? System.Array.Empty<object>());
        }
    }
}
