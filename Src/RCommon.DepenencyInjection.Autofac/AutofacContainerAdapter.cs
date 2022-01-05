using System;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using RCommon.DependencyInjection;
using System.Linq;
using Autofac.Builder;

namespace RCommon.DependencyInjection.Autofac
{
    public class AutofacContainerAdapter : IContainerAdapter
    {
       
        private readonly ContainerBuilder _builder;
        private readonly IServiceCollection _services;

        public IServiceCollection Services => _services;

        

        /// <summary>
        /// Default Constructor.
        /// Creates a new instance of the <see cref="AutofacContainerAdapter"/> class.
        /// </summary>
        /// <param name="builder"></param>
        public AutofacContainerAdapter(ContainerBuilder builder, IServiceCollection services)
        {
            _builder = builder;
            _services = services;
        }

        /// <summary>
        /// Registers a default implementation type for a service type.
        /// </summary>
        /// <typeparam name="TService">The <typeparamref name="TService"/> type representing the service
        /// for which the implementation type is registered. </typeparam>
        /// <typeparam name="TImplementation">The <typeparamref name="TImplementation"/> type representing
        /// the implementation registered for the <typeparamref name="TService"/></typeparam>
        public void Register<TService, TImplementation>() where TImplementation : TService
        {
            _builder.RegisterType<TImplementation>().As<TService>();
        }

        /// <summary>
        /// Registers a named implementation type of a service type.
        /// </summary>
        /// <typeparam name="TService">The <typeparamref name="TService"/> type representing the service
        /// for which the implementation type is registered. </typeparam>
        /// <typeparam name="TImplementation">The <typeparamref name="TImplementation"/> type representing
        /// the implementation registered for the <typeparamref name="TService"/></typeparam>
        /// <param name="named">string. The service name with which the implementation is registered.</param>
        public void Register<TService, TImplementation>(string named) where TImplementation : TService
        {
            _builder.RegisterType<TImplementation>().Named<TService>(named);
        }

        /// <summary>
        /// Registers a default implementation type for a service type.
        /// </summary>
        /// <param name="service"><see cref="Type"/>. The type representing the service for which the
        ///  implementation type is registered.</param>
        /// <param name="implementation"><see cref="Type"/>. The type representing the implementation
        /// registered for the service type.</param>
        public void Register(Type service, Type implementation)
        {
            _builder.RegisterType(implementation).As(service);
        }

        /// <summary>
        /// Registers a named implementation type for a service type.
        /// </summary>
        /// <param name="service"><see cref="Type"/>. The type representing the service for which the
        /// implementation type if registered.</param>
        /// <param name="implementation"><see cref="Type"/>. The type representing the implementaton
        /// registered for the service.</param>
        /// <param name="named">string. The service name with which the implementation is registered.</param>
        public void Register(Type service, Type implementation, string named)
        {
            _builder.RegisterType(implementation).Named(named, service);
        }


        ///<summary>
        /// Registers a named open generic implementation for a generic service type.
        ///</summary>
        ///<param name="service">The type representing the service for which the implementation is registered.</param>
        ///<param name="implementation">The type representing the implementation registered for the service.</param>
        ///<param name="named">string. The service name with which the implementation is registerd.</param>
        public void RegisterGeneric(Type service, Type implementation, string named)
        {
            _builder.RegisterGeneric(service).Named(named, service);
        }

        /// <summary>
        /// Registers a default implementation type for a service type as a singleton.
        /// </summary>
        /// <typeparam name="TService"><typeparamref name="TService"/>. The type representing the service
        /// for which the implementation type is registered as a singleton.</typeparam>
        /// <typeparam name="TImplementation"><typeparamref name="TImplementation"/>. The type representing
        /// the implementation that is registered as a singleton for the service type.</typeparam>
        public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService
        {
            _builder.RegisterType<TImplementation>().As<TService>().SingleInstance();
        }

        /// <summary>
        /// Registers a named implementation type for a service type as a singleton.
        /// </summary>
        /// <typeparam name="TService"><typeparamref name="TService"/>. The type representing the service
        /// for which the implementation type is registered as a singleton.</typeparam>
        /// <typeparam name="TImplementation"><typeparamref name="TImplementation"/>. The type representing
        /// the implementation that is registered as a singleton for the service type.</typeparam>
        /// <param name="named">string. The service name with which the implementation is registerd.</param>
        public void RegisterSingleton<TService, TImplementation>(string named) where TImplementation : TService
        {
            _builder.RegisterType<TImplementation>().Named<TService>(named).SingleInstance();
        }

        /// <summary>
        /// Registers a default implementation type for a service type as a singleton.
        /// </summary>
        /// <param name="service"><see cref="Type"/>. The type representing the service
        /// for which the implementation type is registered as a singleton.</param>
        /// <param name="implementation"><see cref="Type"/>. The type representing
        /// the implementation that is registered as a singleton for the service type.</param>
        public void RegisterSingleton(Type service, Type implementation)
        {
            _builder.RegisterType(implementation).As(service).SingleInstance();
        }

        /// <summary>
        /// Registers a named implementation type for a service type as a singleton.
        /// </summary>
        /// <param name="service"><see cref="Type"/>. The type representing the service
        /// for which the implementation type is registered as a singleton.</param>
        /// <param name="implementation"><see cref="Type"/>. The type representing
        /// the implementation that is registered as a singleton for the service type.</param>
        /// <param name="named">string. The service name with which the implementation is registered.</param>
        public void RegisterSingleton(Type service, Type implementation, string named)
        {
            _builder.RegisterType(implementation).Named(named, service).SingleInstance();
        }

       

        public void AddGeneric(Type service, Type implementation)
        {
            _builder.RegisterGeneric(implementation).As(service);
        }

        public void AddScoped(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            // Not sure if this will work
            _builder.Register<Func<IServiceProvider, object>>(component => provider => 
            component.ResolveKeyed(implementationFactory.Invoke(provider), service))
                .InstancePerLifetimeScope();
        }

        public void AddScoped(Type service, Type implementation)
        {
            _builder.RegisterType(implementation).As(service).InstancePerLifetimeScope();
        }

        public void AddScoped<TService, TImplementation>() where TImplementation : TService
        {
            _builder.RegisterType(typeof(TImplementation)).As(typeof(TService)).InstancePerLifetimeScope();
        }

        public void AddScoped<TService>(Func<IServiceProvider, TService> implementationFactory)
        {
            //_builder.Register<Func<IServiceProvider, TService>>(c => s => c.ResolveKeyed<IServiceProvider>(s)).InstancePerLifetimeScope();
            // Not sure if this will work
            _builder.Register<Func<IServiceProvider, TService>>(component => provider =>
            component.ResolveKeyed<TService>(implementationFactory.Invoke(provider)))
                .InstancePerLifetimeScope();
        }

        public void AddSingleton(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            // Not sure if this will work
            _builder.Register<Func<IServiceProvider, object>>(component => provider =>
            component.ResolveKeyed(implementationFactory.Invoke(provider), service))
                .SingleInstance();
        }

        public void AddSingleton(Type service, Type implementation)
        {
            _builder.RegisterType(implementation).As(service).SingleInstance();
        }

        public void AddSingleton<TService, TImplementation>() where TImplementation : TService
        {
            _builder.RegisterType(typeof(TImplementation)).As(typeof(TService)).SingleInstance();
        }

        public void AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory)
        {
            _builder.Register<Func<IServiceProvider, TService>>(component => provider =>
            component.ResolveKeyed<TService>(implementationFactory.Invoke(provider)))
                .SingleInstance();
        }

        public void AddTransient(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            // Not sure if this will work
            _builder.Register<Func<IServiceProvider, object>>(component => provider =>
            component.ResolveKeyed(implementationFactory.Invoke(provider), service));
        }

        public void AddTransient(Type service, Type implementation)
        {
            _builder.RegisterType(implementation).As(service);
        }

        public void AddTransient<TService, TImplementation>() where TImplementation : TService
        {
            _builder.RegisterType(typeof(TImplementation)).As(typeof(TService));
        }

        public void AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory)
        {
            _builder.Register<Func<IServiceProvider, TService>>(component => provider =>
            component.ResolveKeyed<TService>(implementationFactory.Invoke(provider)));
        }
    }
}
