using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Behaviors;
using RCommon.ApplicationServices.Common;
using RCommon.BusinessServices;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Configuration
{
    public static class CommonApplicationConfigurationExtensions
    {

        public static IRCommonConfiguration WithCrudHelpers(this IRCommonConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(ICrudBusinessService<>), typeof(CrudBusinessService<>));
            config.ContainerAdapter.AddGeneric(typeof(ICrudAppService<,>), typeof(CrudAppService<,>));
            return config;
        }

        public static IRCommonConfiguration WithSmtpEmailServices(this IRCommonConfiguration config, Action<EmailSettings> emailSettings)
        {
            config.ContainerAdapter.Services.Configure<EmailSettings>(emailSettings);
            config.ContainerAdapter.AddTransient<IEmailService, EmailService>();
            return config;
        }

        public static IRCommonConfiguration AddLoggingToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(LoggingBehavior<,>), typeof(IPipelineBehavior<,>));
            return config;
        }

        public static IRCommonConfiguration AddValidationToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(ValidatorBehavior<,,>), typeof(IPipelineBehavior<,>));
            return config;
        }

        public static IRCommonConfiguration AddUnitOfWorkToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(UnitOfWorkBehavior<,>), typeof(IPipelineBehavior<,>));
            return config;
        }

    }
}
