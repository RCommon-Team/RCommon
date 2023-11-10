using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediatr
{
    public class MediatrConfiguration : IMediatrConfiguration
    {
        private readonly IServiceCollection _services;

        public MediatrConfiguration(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IMediatrConfiguration AddMediatr(Action<MediatRServiceConfiguration> options) 
        {
            this._services.AddMediatR(options);
            return this;
        }

        public IMediatrConfiguration AddMediatr(MediatRServiceConfiguration options)
        {
            this._services.AddMediatR(options);
            return this;
        }
    }
}
