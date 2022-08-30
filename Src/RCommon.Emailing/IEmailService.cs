using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RCommon.Emailing
{
    public interface IEmailService
    {
        event EventHandler<EmailEventArgs> EmailSent;

        void SendEmail(MailMessage message);
        Task SendEmailAsync(MailMessage message);
    }
}
