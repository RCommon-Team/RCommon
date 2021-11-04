using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace RCommon.ExceptionHandling.EnterpriseLibraryCore.Handlers
{
    [ConfigurationElementType(typeof(CustomHandlerData))]
    public class LoggingExceptionHandler : IExceptionHandler
    {
        public LoggingExceptionHandler(NameValueCollection vars)
        {

        }

        public Exception HandleException(Exception exception, Guid handlingInstanceId)
        {
            var loggingFactory = new LoggerFactory();
            var logger = loggingFactory.CreateLogger("LoggingExceptionHandler");
            logger.LogError(exception, "A handled exception occured for handlingInstanceId: {0}", handlingInstanceId);
            return exception;
        }
    }
}
