using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        /// <exception cref="RCommonBuilderException">
        /// Thrown when <see cref="IEmailService"/> has already been registered with a different
        /// implementation type, since mixing email service implementations is unsupported.
        /// </exception>
        public static IRCommonBuilder WithSendGridEmailServices(this IRCommonBuilder config, Action<SendGridEmailSettings> emailSettings)
        {
            var existing = config.Services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (existing?.ImplementationType is { } existingImpl && existingImpl != typeof(SendGridEmailService))
            {
                throw new RCommonBuilderException(
                    $"IEmailService already configured as '{existingImpl.FullName}'; cannot reconfigure as '{typeof(SendGridEmailService).FullName}'. " +
                    "To configure multiple modules consistently, ensure all modules agree on the same IEmailService implementation, " +
                    "or designate a single composition root that performs this registration.");
            }

            config.Services.Configure<SendGridEmailSettings>(emailSettings);
            config.Services.TryAddTransient<IEmailService, SendGridEmailService>();
            return config;
        }
    }
}
