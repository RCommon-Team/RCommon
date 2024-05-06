using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RCommon.ApplicationServices.Validation;
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

        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IValidationBuilder
        {

            builder.Services.AddScoped<IValidationService, ValidationService>();

            // Event Handling Configurations 
            var mediatorConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(mediatorConfig);
            return builder;
        }

        public static void UseWithCqrs(this IValidationBuilder builder, Action<CqrsValidationOptions> options)
        {
            builder.Services.Configure<CqrsValidationOptions>(options);
        }

    }
}
