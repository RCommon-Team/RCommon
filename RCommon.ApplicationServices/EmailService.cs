using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net;

namespace RCommon.ApplicationServices
{
    public class EmailService : IEmailService
    {


        public EmailService()
        {

        }


        /// <summary>
        /// Sends a MailMessage object using the SMTP settings.
        /// </summary>
        public void SendEmail(MailMessage message, EmailSettings settings)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            try
            {
                message.BodyEncoding = Encoding.UTF8;
                using (var smtp = new SmtpClient())
                {
                    smtp.Credentials = new NetworkCredential(settings.UserName, settings.Password);
                    smtp.Host = settings.Host;
                    smtp.Port = settings.Port;
                    smtp.EnableSsl = settings.EnableSsl;
                    smtp.Send(message);
                }
                OnEmailSent(message);
            }
            finally
            {
                // Remove the pointer to the message object so the GC can close the thread.
                message.Dispose();
                message = null;
            }
        }

        /// <summary>
        /// Sends the mail message asynchronously in another thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public async Task SendEmailAsync(MailMessage message, EmailSettings settings)
        {
            
            await Task.Run(() => SendEmail(message, settings));
        }

        /// <summary>
        /// Occurs after an e-mail has been sent. The sender is the MailMessage object.
        /// </summary>
        public event EventHandler<EventArgs> EmailSent;
        private void OnEmailSent(MailMessage message)
        {
            if (EmailSent != null)
            {
                EmailSent(message, new EventArgs());
            }
        }




    }
}
