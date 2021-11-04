using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ApplicationServices.Common
{
    public class EmailSettings
    {
        public EmailSettings()
        {

        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public string From { get; set; }
    }
}
