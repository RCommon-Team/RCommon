using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Exception thrown when the <see cref="RCommonBuilder"/> encounters a configuration error,
    /// such as attempting to configure a service that has already been configured.
    /// Always has <see cref="SeverityOptions.Critical"/> severity.
    /// </summary>
    public  class RCommonBuilderException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RCommonBuilderException"/> with the specified message
        /// and <see cref="SeverityOptions.Critical"/> severity.
        /// </summary>
        /// <param name="message">The error message describing the configuration failure.</param>
        public RCommonBuilderException(string message) : base(SeverityOptions.Critical , message)
        {

        }
    }
}
