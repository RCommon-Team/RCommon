using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Emailing.Smtp
{
    /// <summary>
    /// Configuration settings for the <see cref="SmtpEmailService"/>.
    /// Typically bound from an application configuration section (e.g., appsettings.json).
    /// </summary>
    public class SmtpEmailSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmtpEmailSettings"/> class.
        /// </summary>
        public SmtpEmailSettings()
        {

        }

        /// <summary>
        /// Gets or sets the SMTP authentication user name.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the SMTP authentication password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL is enabled for the SMTP connection.
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// Gets or sets the SMTP server port number.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the SMTP server host name or IP address.
        /// </summary>
        public string? Host { get; set; }

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
