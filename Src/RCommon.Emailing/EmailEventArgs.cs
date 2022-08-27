using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Emailing
{
    public class EmailEventArgs : EventArgs
    {
        public EmailEventArgs(MailMessage mailMessage)
        {
            MailMessage=mailMessage;
        }

        public MailMessage MailMessage { get; }
    }
}
