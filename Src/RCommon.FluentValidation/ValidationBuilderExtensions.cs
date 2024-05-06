using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public static class ValidationBuilderExtensions
    {

        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder)
            where T : IValidationBuilder
        {

            return WithValidation<T>(builder, x => { });
        }

        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder, Action<CqrsValidationOptions> actions)
            where T : IValidationBuilder
        {

            // Event Handling Configurations 
            var cqrsBuilder = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            builder.Services.Configure<CqrsValidationOptions>(actions);
            return builder;
        }
    }
}
