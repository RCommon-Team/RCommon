using Microsoft.Extensions.DependencyInjection;
using RCommon.Emailing;
using RCommon.Emailing.SendGrid;
using RCommon.Emailing.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Extension methods for <see cref="IRCommonBuilder"/> that register SendGrid-based email services.
    /// </summary>
    public static class SendGridEmailingConfigurationExtensions
    {
        /// <summary>
        /// Registers <see cref="SendGridEmailService"/> as the <see cref="IEmailService"/> implementation
        /// and configures the SendGrid settings via the provided delegate.
        /// </summary>
        /// <param name="config">The RCommon builder to configure.</param>
        /// <param name="emailSettings">A delegate to configure <see cref="SendGridEmailSettings"/>.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> instance for fluent chaining.</returns>
        public static IRCommonBuilder WithSendGridEmailServices(this IRCommonBuilder config, Action<SendGridEmailSettings> emailSettings)
        {
            config.Services.Configure<SendGridEmailSettings>(emailSettings);
            config.Services.AddTransient<IEmailService, SendGridEmailService>();
            return config;
        }
    }
}
