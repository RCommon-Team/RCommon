using Microsoft.Extensions.Options;
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

        public SendGridEmailService(IOptions<EmailSettings> settings)
        {
                
        }

        public event EventHandler<EventArgs> EmailSent;

        public void SendEmail(MailMessage message)
        {
            throw new NotImplementedException();
        }

        public Task SendEmailAsync(MailMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
