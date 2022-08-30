using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Extensions.Options;

namespace RCommon.Emailing.Smtp
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpEmailSettings _settings;

        public SmtpEmailService(IOptions<SmtpEmailSettings> settings)
        {
            _settings=settings.Value;
        }


        /// <summary>
        /// Sends a MailMessage object using the SMTP settings.
        /// </summary>
        public void SendEmail(MailMessage message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            try
            {
                message.BodyEncoding = Encoding.UTF8;
                using (var smtp = new SmtpClient())
                {
                    smtp.Credentials = new NetworkCredential(this._settings.UserName, this._settings.Password);
                    smtp.Host = this._settings.Host;
                    smtp.Port = this._settings.Port;
                    smtp.EnableSsl = this._settings.EnableSsl;
                    smtp.Send(message);
                }
                OnEmailSent(message);
            }
            finally
            {
                // Remove the pointer to the message object so the GC can close the thread.
                message.Dispose();
            }
        }

        /// <summary>
        /// Sends the mail message asynchronously in another thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public async Task SendEmailAsync(MailMessage message)
        {
            
            await Task.Run(() => SendEmail(message));
        }

        /// <summary>
        /// Occurs after an e-mail has been sent. The sender is the MailMessage object.
        /// </summary>
        public event EventHandler<EmailEventArgs> EmailSent;
        private void OnEmailSent(MailMessage message)
        {
            if (EmailSent != null)
            {
                EmailSent(message, new EmailEventArgs(message));
            }
        }

    }
}
