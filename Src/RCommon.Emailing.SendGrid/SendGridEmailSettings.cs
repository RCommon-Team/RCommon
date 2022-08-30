using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Emailing.SendGrid
{
    public class SendGridEmailSettings
    {

        public SendGridEmailSettings()
        {

        }

        public string SendGridApiKey { get; set; }
        public string FromEmailDefault { get; set; }
        public string FromNameDefault { get; set; }
    }
}
