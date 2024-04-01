using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class MediatorBuilderExtensions
    {
        public static IRCommonBuilder WithMediator<T>(this IRCommonBuilder builder)
            where T : IMediatorBuilder
        {
            return WithMediator<T>(builder, x => { });
        }

        public static IRCommonBuilder WithMediator<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IMediatorBuilder
        {

            builder.Services.AddSingleton<IMediatorService, MediatorService>();

            // Event Handling Configurations 
            var mediatorConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(mediatorConfig);
            return builder;
        }

    }
}
