using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace RCommon.FluentValidation
{
    public class FluentValidationBuilder : IFluentValidationBuilder
    {
        public FluentValidationBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        protected void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IValidationProvider, FluentValidationProvider>();
        }

        public IServiceCollection Services { get; }
    }
}
