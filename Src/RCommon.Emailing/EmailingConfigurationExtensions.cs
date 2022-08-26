using Microsoft.Extensions.DependencyInjection;
using RCommon.Configuration;
using RCommon.Emailing;
using RCommon.Emailing.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class EmailingConfigurationExtensions
    {

        public static IRCommonConfiguration WithSmtpEmailServices(this IRCommonConfiguration config, Action<EmailSettings> emailSettings)
        {
            config.ContainerAdapter.Services.Configure<EmailSettings>(emailSettings);
            config.ContainerAdapter.AddTransient<IEmailService, SmtpEmailService>();
            return config;
        }
    }
}
