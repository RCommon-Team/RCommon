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
    public class SendGridEmailService : IEmailService
    {

        public event EventHandler<EmailEventArgs> EmailSent;

        public void SendEmail(MailMessage message)
        {
            AsyncHelper.RunSync(() => SendEmailAsync(message));
        }

        private readonly SendGridClient _client;
        
        public SendGridEmailService(IOptions<SendGridEmailSettings> settings, ILogger<SendGridEmailService> logger) 
        { 
            _client = new SendGridClient(settings.Value.SendGridApiKey); 
        }

        private void OnEmailSent(MailMessage message)
        {
            if (EmailSent != null)
            {
                EmailSent(message, new EmailEventArgs(message));
            }
        }

        public async Task SendEmailAsync(MailMessage message)
        {
            if (message.To.Count == 0)
            {
                await Task.CompletedTask;
            }

            var sgMessage = MailHelper.CreateSingleEmailToMultipleRecipients(from: new EmailAddress(message.From.Address, message.From.DisplayName),
                tos: message.To.Select(r => new EmailAddress(r.Address, r.DisplayName)).ToList(),
                subject: message.Subject,
                plainTextContent: message.IsBodyHtml ? null :
                message.Body,
                htmlContent: message.IsBodyHtml ? message.Body : null);

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
