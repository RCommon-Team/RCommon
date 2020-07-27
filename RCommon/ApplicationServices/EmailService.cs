using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        public void SendEmail(MailMessage message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            try
            {
                message.BodyEncoding = Encoding.UTF8;
                SmtpClient smtp = new SmtpClient();
                //smtp.Credentials = new System.Net.NetworkCredential(ConfigurationSectionManager.GetAppSetting("MailUserName"), ConfigurationSectionManager.GetAppSetting("MailPassword"));
                //smtp.Port = int.Parse(ConfigurationSectionManager.GetAppSetting("MailPort"));
                //smtp.EnableSsl = false;
                smtp.Send(message);
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
        public Task SendEmailAsync(MailMessage message)
        {
            
            Task.Run(() => SendEmail(message));
            return Task.CompletedTask;
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
