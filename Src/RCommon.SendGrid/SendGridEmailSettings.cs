using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Emailing.SendGrid
{
    /// <summary>
    /// Configuration settings for the <see cref="SendGridEmailService"/>.
    /// Typically bound from an application configuration section (e.g., appsettings.json).
    /// </summary>
    public class SendGridEmailSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailSettings"/> class.
        /// </summary>
        public SendGridEmailSettings()
        {

        }

        /// <summary>
        /// Gets or sets the SendGrid API key used to authenticate with the SendGrid service.
        /// </summary>
        public string? SendGridApiKey { get; set; }

        /// <summary>
        /// Gets or sets the default sender email address used when no explicit "From" address is specified.
        /// </summary>
        public string? FromEmailDefault { get; set; }

        /// <summary>
        /// Gets or sets the default sender display name used when no explicit "From" name is specified.
        /// </summary>
        public string? FromNameDefault { get; set; }
    }
}
