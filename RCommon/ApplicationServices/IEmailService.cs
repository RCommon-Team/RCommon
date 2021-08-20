using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public interface IEmailService
    {
        event EventHandler<EventArgs> EmailSent;

        void SendEmail(MailMessage message, EmailSettings settings);
        Task SendEmailAsync(MailMessage message, EmailSettings settings);
    }
}