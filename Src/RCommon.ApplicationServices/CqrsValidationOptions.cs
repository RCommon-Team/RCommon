using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Configuration options that control whether automatic validation is applied to CQRS commands and queries.
    /// </summary>
    /// <remarks>
    /// When enabled, the <see cref="Commands.CommandBus"/> and <see cref="Queries.QueryBus"/> will invoke
    /// <see cref="Validation.IValidationService"/> before dispatching to handlers.
    /// Both options default to <c>false</c>.
    /// </remarks>
    public class CqrsValidationOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CqrsValidationOptions"/> with validation disabled for both commands and queries.
        /// </summary>
        public CqrsValidationOptions()
        {
            ValidateQueries = false;
            ValidateCommands = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether queries should be validated before dispatch.
        /// </summary>
        public bool ValidateQueries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether commands should be validated before dispatch.
        /// </summary>
        public bool ValidateCommands { get; set; }
    }
}
