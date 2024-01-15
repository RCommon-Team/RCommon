using Microsoft.Extensions.DependencyInjection;
using RCommon.Emailing;
using RCommon.Emailing.SendGrid;
using RCommon.Emailing.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class SendGridEmailingConfigurationExtensions
    {

        public static IRCommonBuilder WithSendGridEmailServices(this IRCommonBuilder config, Action<SendGridEmailSettings> emailSettings)
        {
            config.Services.Configure<SendGridEmailSettings>(emailSettings);
            config.Services.AddTransient<IEmailService, SendGridEmailService>();
            return config;
        }
    }
}
