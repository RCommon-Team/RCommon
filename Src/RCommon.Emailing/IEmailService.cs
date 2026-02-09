using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RCommon.Emailing
{
    /// <summary>
    /// Defines a service for sending email messages, with support for both synchronous and asynchronous delivery.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Occurs after an email has been successfully sent.
        /// </summary>
        event EventHandler<EmailEventArgs> EmailSent;

        /// <summary>
        /// Sends the specified <paramref name="message"/> synchronously.
        /// </summary>
        /// <param name="message">The <see cref="MailMessage"/> to send.</param>
        void SendEmail(MailMessage message);

        /// <summary>
        /// Sends the specified <paramref name="message"/> asynchronously.
        /// </summary>
        /// <param name="message">The <see cref="MailMessage"/> to send.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous send operation.</returns>
        Task SendEmailAsync(MailMessage message);
    }
}
