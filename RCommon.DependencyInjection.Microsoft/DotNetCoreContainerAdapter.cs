using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RCommon.DependencyInjection.Microsoft
{
    public class DotNetCoreContainerAdapter : IContainerAdapter
    {
        private IServiceCollection _services;

        public DotNetCoreContainerAdapter(IServiceCollection services)
        {
            _services = services;
        }


        public void AddGeneric(Type service, Type implementation)
        {
            _services.AddTransient(service, implementation);
        }


        public void AddScoped<TService, TImplementation>() where TImplementation : TService
        {
            _services.AddScoped(typeof(TService), typeof(TImplementation));
        }

        public void AddScoped(Type service, Type implementation)
        {
            _services.AddScoped(service, implementation);
        }

        public void AddScoped(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            _services.AddScoped(service, implementationFactory);
        }

        public void AddScoped<TService>(Func<IServiceProvider, TService> implementationFactory)
        {
            _services.AddScoped(typeof(TService), implementationFactory as Func<IServiceProvider, object>);
        }

        public void AddSingleton<TService, TImplementation>() where TImplementation : TService
        {
            _services.AddSingleton(typeof(TService), typeof(TImplementation));
        }

        public void AddSingleton(Type service, Type implementation)
        {
            _services.AddSingleton(service, implementation);
        }

        public void AddSingleton(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            _services.AddSingleton(service, implementationFactory);
        }

        public void AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory)
        {
            _services.AddSingleton(typeof(TService), implementationFactory as Func<IServiceProvider, object>);
        }

        public void AddTransient<TService, TImplementation>() where TImplementation : TService
        {
            _services.AddTransient(typeof(TService), typeof(TImplementation));
        }

        public void AddTransient(Type service, Type implementation)
        {
            _services.AddTransient(service, implementation);
        }

        public void AddTransient(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            _services.AddTransient(service, implementationFactory);
        }

        public void AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory)
        {
            _services.AddTransient(typeof(TService), implementationFactory as Func<IServiceProvider, object>);
        }
    }
}
