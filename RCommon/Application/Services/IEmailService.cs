using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RCommon.Application.Services
{
    public interface IEmailService
    {
        event EventHandler<EventArgs> EmailSent;

        void SendEmail(MailMessage message, string mailUserName, string mailPassword, int port, bool enableSsl);
        Task SendEmailAsync(MailMessage message, string mailUserName, string mailPassword, int port, bool enableSsl);
    }
}