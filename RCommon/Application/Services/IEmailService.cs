using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RCommon.Application.Services
{
    public interface IEmailService
    {
        event EventHandler<EventArgs> EmailSent;

        void SendEmail(MailMessage message);
        Task SendEmailAsync(MailMessage message);
    }
}