using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Core.Threading;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Emailing.SendGrid
{
    /// <summary>
    /// Implementation of <see cref="IEmailService"/> that sends email through the SendGrid API.
    /// </summary>
    /// <remarks>
    /// Converts standard <see cref="MailMessage"/> objects to SendGrid messages. The API key is
    /// configured via <see cref="SendGridEmailSettings"/>.
    /// </remarks>
    public class SendGridEmailService : IEmailService
    {
        /// <inheritdoc />
        public event EventHandler<EmailEventArgs>? EmailSent;

        /// <inheritdoc />
        /// <remarks>
        /// Synchronous wrapper that delegates to <see cref="SendEmailAsync"/> using <see cref="AsyncHelper.RunSync"/>.
        /// </remarks>
        public void SendEmail(MailMessage message)
        {
            AsyncHelper.RunSync(() => SendEmailAsync(message));
        }

        private readonly SendGridClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailService"/> class.
        /// </summary>
        /// <param name="settings">The SendGrid configuration options containing the API key.</param>
        /// <param name="logger">The logger instance for diagnostic output.</param>
        public SendGridEmailService(IOptions<SendGridEmailSettings> settings, ILogger<SendGridEmailService> logger)
        {
            _client = new SendGridClient(settings.Value.SendGridApiKey);
        }

        /// <summary>
        /// Raises the <see cref="EmailSent"/> event if any subscribers are attached.
        /// </summary>
        /// <param name="message">The mail message that was sent.</param>
        private void OnEmailSent(MailMessage message)
        {
            if (EmailSent != null)
            {
                EmailSent(message, new EmailEventArgs(message));
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Converts the <see cref="MailMessage"/> to a SendGrid <see cref="SendGridMessage"/>,
        /// streams any attachments, then sends via the SendGrid API client.
        /// </remarks>
        public async Task SendEmailAsync(MailMessage message)
        {
            // No-op when there are no recipients.
            if (message.To.Count == 0)
            {
                await Task.CompletedTask;
            }

            // Map the MailMessage to a SendGrid message, choosing plain text or HTML based on IsBodyHtml.
            var sgMessage = MailHelper.CreateSingleEmailToMultipleRecipients(from: new EmailAddress(message.From!.Address, message.From.DisplayName),
                tos: message.To.Select(r => new EmailAddress(r.Address, r.DisplayName)).ToList(),
                subject: message.Subject,
                plainTextContent: message.IsBodyHtml ? null :
                message.Body,
                htmlContent: message.IsBodyHtml ? message.Body : null);

            // Stream each attachment into the SendGrid message.
            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    await sgMessage.AddAttachmentAsync(attachment.Name, attachment.ContentStream);
                }
            }
            await _client.SendEmailAsync(sgMessage);

            OnEmailSent(message);
        }
    }


}
