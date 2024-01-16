using Microsoft.Extensions.DependencyInjection;
using RCommon.Emailing;
using RCommon.Emailing.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class EmailingBuilderExtensions
    {

        public static IRCommonBuilder WithSmtpEmailServices(this IRCommonBuilder config, Action<SmtpEmailSettings> emailSettings)
        {
            config.Services.Configure<SmtpEmailSettings>(emailSettings);
            config.Services.AddTransient<IEmailService, SmtpEmailService>();
            return config;
        }
    }
}
