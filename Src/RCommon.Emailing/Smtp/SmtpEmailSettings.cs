using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Emailing.Smtp
{
    public class SmtpEmailSettings
    {
        public SmtpEmailSettings()
        {

        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public string FromEmailDefault { get; set; }
        public string FromNameDefault { get; set; }

    }
}
