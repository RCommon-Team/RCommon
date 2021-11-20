using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Common
{
    public interface IEmailService
    {
        event EventHandler<EventArgs> EmailSent;

        void SendEmail(MailMessage message);
        Task SendEmailAsync(MailMessage message);
    }
}
