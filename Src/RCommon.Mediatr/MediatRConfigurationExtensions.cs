﻿using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.MediatR.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public static class MediatrConfigurationExtensions
    {


        public static IRCommonConfiguration AddMediatR(this IRCommonConfiguration config, Action<MediatRServiceConfiguration> mediatrOptions )
        {
            config.Services.AddMediatR(mediatrOptions);
            return config;
        }

        public static IRCommonConfiguration AddLoggingToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            return config;
        }

        public static IRCommonConfiguration AddValidationToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
            return config;
        }

        public static IRCommonConfiguration AddUnitOfWorkToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
            return config;
        }

        public static IRCommonConfiguration AddDisributedUnitOfWorkToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DistributedUnitOfWorkBehavior<,>));
            return config;
        }

    }
}
