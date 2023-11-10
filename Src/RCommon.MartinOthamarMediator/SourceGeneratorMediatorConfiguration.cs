using Mediator;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MartinOthamarMediator
{
    public class SourceGeneratorMediatorConfiguration : ISourceGeneratorMediatorConfiguration
    {
        private readonly IServiceCollection _services;

        public SourceGeneratorMediatorConfiguration(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _services.AddTransient<RCommon.Mediator.IMediatorService, SourceGeneratorMediatorService>();
        }

        public ISourceGeneratorMediatorConfiguration AddSourceGeneratorMediator()
        {
            this._services.AddMediator();
            return this;
        }

        public ISourceGeneratorMediatorConfiguration AddSourceGeneratorMediator(Action<MediatorOptions> options)
        {
            this._services.AddMediator(options);
            return this;   
        }
    }
}
