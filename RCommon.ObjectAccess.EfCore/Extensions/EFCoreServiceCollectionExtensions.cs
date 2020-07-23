// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon;
using RCommon.DataServices;
using RCommon.ObjectAccess.EFCore;

// ReSharper disable once CheckNamespace
namespace RCommon.ObjectAccess.EFCore
{
    /// <summary>
    ///     Extension methods for setting up Entity Framework related services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class EFCoreServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the given context as a service in the <see cref="IServiceCollection" />.
        ///     You use this method when using dependency injection in your application, such as with ASP.NET.
        ///     For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        /// </summary>
        /// <example>
        ///     <code>
        ///           public void ConfigureServices(IServiceCollection services)
        ///           {
        ///               var connectionString = "connection string to database";
        ///
        ///               services.AddDbContext&lt;MyContext&gt;(options => options.UseSqlServer(connectionString));
        ///           }
        ///       </code>
        /// </example>
        /// <typeparam name="TContext"> The type of context to be registered. </typeparam>
        /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="optionsAction">
        ///     <para>
        ///         An optional action to configure the <see cref="DataStoreDbContextOptions" /> for the context. This provides an
        ///         alternative to performing configuration of the context by overriding the
        ///         <see cref="DbContext.OnConfiguring" /> method in your derived context.
        ///     </para>
        ///     <para>
        ///         If an action is supplied here, the <see cref="DbContext.OnConfiguring" /> method will still be run if it has
        ///         been overridden on the derived context. <see cref="DbContext.OnConfiguring" /> configuration will be applied
        ///         in addition to configuration performed here.
        ///     </para>
        ///     <para>
        ///         In order for the options to be passed into your context, you need to expose a constructor on your context that takes
        ///         <see cref="DataStoreDbContextOptions{TContext}" /> and passes it to the base constructor of <see cref="DbContext" />.
        ///     </para>
        /// </param>
        /// <param name="contextLifetime"> The lifetime with which to register the DbContext service in the container. </param>
        /// <param name="optionsLifetime"> The lifetime with which to register the DataStoreDbContextOptions service in the container. </param>
        /// <returns>
        ///     The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddDbContext<TContext>(
            [NotNull] this IServiceCollection serviceCollection,
            [CanBeNull] Action<DataStoreDbContextOptionsBuilder> optionsAction = null,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContext : RCommonDbContext
            => AddDbContext<TContext, TContext>(serviceCollection, optionsAction, contextLifetime, optionsLifetime);

        /// <summary>
        ///     Registers the given context as a service in the <see cref="IServiceCollection" />.
        ///     You use this method when using dependency injection in your application, such as with ASP.NET.
        ///     For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        /// </summary>
        /// <example>
        ///     <code>
        ///           public void ConfigureServices(IServiceCollection services)
        ///           {
        ///               var connectionString = "connection string to database";
        ///
        ///               services.AddDbContext&lt;MyContext&gt;(options => options.UseSqlServer(connectionString));
        ///           }
        ///       </code>
        /// </example>
        /// <typeparam name="TContextService"> The class or interface that will be used to resolve the context from the container. </typeparam>
        /// <typeparam name="TContextImplementation"> The concrete implementation type to create. </typeparam>
        /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="optionsAction">
        ///     <para>
        ///         An optional action to configure the <see cref="DataStoreDbContextOptions" /> for the context. This provides an
        ///         alternative to performing configuration of the context by overriding the
        ///         <see cref="DbContext.OnConfiguring" /> method in your derived context.
        ///     </para>
        ///     <para>
        ///         If an action is supplied here, the <see cref="DbContext.OnConfiguring" /> method will still be run if it has
        ///         been overridden on the derived context. <see cref="DbContext.OnConfiguring" /> configuration will be applied
        ///         in addition to configuration performed here.
        ///     </para>
        ///     <para>
        ///         In order for the options to be passed into your context, you need to expose a constructor on your context that takes
        ///         <see cref="DataStoreDbContextOptions{TContext}" /> and passes it to the base constructor of <see cref="DbContext" />.
        ///     </para>
        /// </param>
        /// <param name="contextLifetime"> The lifetime with which to register the DbContext service in the container. </param>
        /// <param name="optionsLifetime"> The lifetime with which to register the DataStoreDbContextOptions service in the container. </param>
        /// <returns>
        ///     The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddDbContext<TContextService, TContextImplementation>(
            [NotNull] this IServiceCollection serviceCollection,
            [CanBeNull] Action<DataStoreDbContextOptionsBuilder> optionsAction = null,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContextImplementation : RCommonDbContext, TContextService
            => AddDbContext<TContextService, TContextImplementation>(
                serviceCollection,
                optionsAction == null
                    ? (Action<IServiceProvider, DataStoreDbContextOptionsBuilder>)null
                    : (p, b) => optionsAction.Invoke(b), contextLifetime, optionsLifetime);

        

        

        /// <summary>
        ///     Registers the given context as a service in the <see cref="IServiceCollection" />.
        ///     You use this method when using dependency injection in your application, such as with ASP.NET.
        ///     For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        /// </summary>
        /// <example>
        ///     <code>
        ///           public void ConfigureServices(IServiceCollection services)
        ///           {
        ///               var connectionString = "connection string to database";
        ///
        ///               services.AddDbContext&lt;MyContext&gt;(ServiceLifetime.Scoped);
        ///           }
        ///       </code>
        /// </example>
        /// <typeparam name="TContextService"> The class or interface that will be used to resolve the context from the container. </typeparam>
        /// <typeparam name="TContextImplementation"> The concrete implementation type to create. </typeparam>
        /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="contextLifetime"> The lifetime with which to register the DbContext service in the container. </param>
        /// <param name="optionsLifetime"> The lifetime with which to register the DataStoreDbContextOptions service in the container. </param>
        /// <returns>
        ///     The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddDbContext<TContextService, TContextImplementation>(
            [NotNull] this IServiceCollection serviceCollection,
            ServiceLifetime contextLifetime,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContextImplementation : RCommonDbContext, TContextService
            where TContextService : class
            => AddDbContext<TContextService, TContextImplementation>(
                serviceCollection,
                (Action<IServiceProvider, DataStoreDbContextOptionsBuilder>)null,
                contextLifetime,
                optionsLifetime);

        /// <summary>
        ///     <para>
        ///         Registers the given context as a service in the <see cref="IServiceCollection" />.
        ///         You use this method when using dependency injection in your application, such as with ASP.NET.
        ///         For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        ///     </para>
        ///     <para>
        ///         This overload has an <paramref name="optionsAction" /> that provides the applications <see cref="IServiceProvider" />.
        ///         This is useful if you want to setup Entity Framework to resolve its internal services from the primary application service
        ///         provider.
        ///         By default, we recommend using the other overload, which allows Entity Framework to create and maintain its own
        ///         <see cref="IServiceProvider" />
        ///         for internal Entity Framework services.
        ///     </para>
        /// </summary>
        /// <example>
        ///     <code>
        ///           public void ConfigureServices(IServiceCollection services)
        ///           {
        ///               var connectionString = "connection string to database";
        ///
        ///               services
        ///                   .AddEntityFrameworkSqlServer()
        ///                   .AddDbContext&lt;MyContext&gt;((serviceProvider, options) =>
        ///                       options.UseSqlServer(connectionString)
        ///                              .UseInternalServiceProvider(serviceProvider));
        ///           }
        ///       </code>
        /// </example>
        /// <typeparam name="TContext"> The type of context to be registered. </typeparam>
        /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="optionsAction">
        ///     <para>
        ///         An optional action to configure the <see cref="DataStoreDbContextOptions" /> for the context. This provides an
        ///         alternative to performing configuration of the context by overriding the
        ///         <see cref="DbContext.OnConfiguring" /> method in your derived context.
        ///     </para>
        ///     <para>
        ///         If an action is supplied here, the <see cref="DbContext.OnConfiguring" /> method will still be run if it has
        ///         been overridden on the derived context. <see cref="DbContext.OnConfiguring" /> configuration will be applied
        ///         in addition to configuration performed here.
        ///     </para>
        ///     <para>
        ///         In order for the options to be passed into your context, you need to expose a constructor on your context that takes
        ///         <see cref="DataStoreDbContextOptions{TContext}" /> and passes it to the base constructor of <see cref="DbContext" />.
        ///     </para>
        /// </param>
        /// <param name="contextLifetime"> The lifetime with which to register the DbContext service in the container. </param>
        /// <param name="optionsLifetime"> The lifetime with which to register the DataStoreDbContextOptions service in the container. </param>
        /// <returns>
        ///     The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddDbContext<TContext>(
            [NotNull] this IServiceCollection serviceCollection,
            [CanBeNull] Action<IServiceProvider, DataStoreDbContextOptionsBuilder> optionsAction,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContext : RCommonDbContext
            => AddDbContext<TContext, TContext>(serviceCollection, optionsAction, contextLifetime, optionsLifetime);

        /// <summary>
        ///     <para>
        ///         Registers the given context as a service in the <see cref="IServiceCollection" />.
        ///         You use this method when using dependency injection in your application, such as with ASP.NET.
        ///         For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        ///     </para>
        ///     <para>
        ///         This overload has an <paramref name="optionsAction" /> that provides the applications <see cref="IServiceProvider" />.
        ///         This is useful if you want to setup Entity Framework to resolve its internal services from the primary application service
        ///         provider.
        ///         By default, we recommend using the other overload, which allows Entity Framework to create and maintain its own
        ///         <see cref="IServiceProvider" />
        ///         for internal Entity Framework services.
        ///     </para>
        /// </summary>
        /// <example>
        ///     <code>
        ///           public void ConfigureServices(IServiceCollection services)
        ///           {
        ///               var connectionString = "connection string to database";
        ///
        ///               services
        ///                   .AddEntityFrameworkSqlServer()
        ///                   .AddDbContext&lt;MyContext&gt;((serviceProvider, options) =>
        ///                       options.UseSqlServer(connectionString)
        ///                              .UseInternalServiceProvider(serviceProvider));
        ///           }
        ///       </code>
        /// </example>
        /// <typeparam name="TContextService"> The class or interface that will be used to resolve the context from the container. </typeparam>
        /// <typeparam name="TContextImplementation"> The concrete implementation type to create. </typeparam>
        /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="optionsAction">
        ///     <para>
        ///         An optional action to configure the <see cref="DataStoreDbContextOptions" /> for the context. This provides an
        ///         alternative to performing configuration of the context by overriding the
        ///         <see cref="DbContext.OnConfiguring" /> method in your derived context.
        ///     </para>
        ///     <para>
        ///         If an action is supplied here, the <see cref="DbContext.OnConfiguring" /> method will still be run if it has
        ///         been overridden on the derived context. <see cref="DbContext.OnConfiguring" /> configuration will be applied
        ///         in addition to configuration performed here.
        ///     </para>
        ///     <para>
        ///         In order for the options to be passed into your context, you need to expose a constructor on your context that takes
        ///         <see cref="DataStoreDbContextOptions{TContext}" /> and passes it to the base constructor of <see cref="DbContext" />.
        ///     </para>
        /// </param>
        /// <param name="contextLifetime"> The lifetime with which to register the DbContext service in the container. </param>
        /// <param name="optionsLifetime"> The lifetime with which to register the DataStoreDbContextOptions service in the container. </param>
        /// <returns>
        ///     The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddDbContext<TContextService, TContextImplementation>(
            [NotNull] this IServiceCollection serviceCollection,
            [CanBeNull] Action<IServiceProvider, DataStoreDbContextOptionsBuilder> optionsAction,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContextImplementation : RCommonDbContext, TContextService
        {
            Guard.IsNotNull(serviceCollection, nameof(serviceCollection));

            if (contextLifetime == ServiceLifetime.Singleton)
            {
                optionsLifetime = ServiceLifetime.Singleton;
            }

            if (optionsAction != null)
            {
                CheckContextConstructors<TContextImplementation>();
            }

            AddCoreServices<TContextImplementation>(serviceCollection, optionsAction, optionsLifetime);

            serviceCollection.TryAdd(new ServiceDescriptor(typeof(TContextService), typeof(TContextImplementation), contextLifetime));

            return serviceCollection;
        }

        private static void AddCoreServices<TContextImplementation>(
            IServiceCollection serviceCollection,
            Action<IServiceProvider, DataStoreDbContextOptionsBuilder> optionsAction,
            ServiceLifetime optionsLifetime)
            where TContextImplementation : RCommonDbContext
        {
            serviceCollection.TryAdd(
                new ServiceDescriptor(
                    typeof(DataStoreDbContextOptions<TContextImplementation>),
                    p => CreateDataStoreDbContextOptions<TContextImplementation>(p, optionsAction),
                    optionsLifetime));

            serviceCollection.Add(
                new ServiceDescriptor(
                    typeof(DataStoreDbContextOptions),
                    p => p.GetRequiredService<DataStoreDbContextOptions<TContextImplementation>>(),
                    optionsLifetime));
        }

        private static DataStoreDbContextOptions<TContext> CreateDataStoreDbContextOptions<TContext>(
            [NotNull] IServiceProvider applicationServiceProvider,
            [CanBeNull] Action<IServiceProvider, DataStoreDbContextOptionsBuilder> optionsAction)
            where TContext : RCommonDbContext
        {
            var builder = new DataStoreDbContextOptionsBuilder<TContext>(
                new DataStoreDbContextOptions<TContext>(new Dictionary<Type, IDbContextOptionsExtension>()));

            builder.UseApplicationServiceProvider(applicationServiceProvider);

            optionsAction?.Invoke(applicationServiceProvider, builder);

            return builder.Options;
        }

        private static void CheckContextConstructors<TContext>()
            where TContext : RCommonDbContext
        {
            var declaredConstructors = typeof(TContext).GetTypeInfo().DeclaredConstructors.ToList();
            if (declaredConstructors.Count == 1
                && declaredConstructors[0].GetParameters().Length == 0)
            {
                throw new ArgumentException(CoreStrings.DbContextMissingConstructor(typeof(TContext).ShortDisplayName()));
            }
        }
    }
}