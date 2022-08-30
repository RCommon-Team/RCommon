using Microsoft.Extensions.DependencyInjection;
using RCommon.Configuration;
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

        public static IRCommonConfiguration WithSendGridEmailServices(this IRCommonConfiguration config, Action<SendGridEmailSettings> emailSettings)
        {
            config.ContainerAdapter.Services.Configure<SendGridEmailSettings>(emailSettings);
            config.ContainerAdapter.AddTransient<IEmailService, SendGridEmailService>();
            return config;
        }
    }
}
