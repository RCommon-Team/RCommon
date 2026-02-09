using Microsoft.Extensions.DependencyInjection;
using RCommon.Emailing;
using RCommon.Emailing.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Extension methods for <see cref="IRCommonBuilder"/> that register SMTP-based email services.
    /// </summary>
    public static class EmailingBuilderExtensions
    {
        /// <summary>
        /// Registers <see cref="SmtpEmailService"/> as the <see cref="IEmailService"/> implementation
        /// and configures the SMTP settings via the provided delegate.
        /// </summary>
        /// <param name="config">The RCommon builder to configure.</param>
        /// <param name="emailSettings">A delegate to configure <see cref="SmtpEmailSettings"/>.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> instance for fluent chaining.</returns>
        public static IRCommonBuilder WithSmtpEmailServices(this IRCommonBuilder config, Action<SmtpEmailSettings> emailSettings)
        {
            config.Services.Configure<SmtpEmailSettings>(emailSettings);
            config.Services.AddTransient<IEmailService, SmtpEmailService>();
            return config;
        }
    }
}
