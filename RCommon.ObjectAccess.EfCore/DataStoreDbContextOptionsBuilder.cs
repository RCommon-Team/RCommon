// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.DataServices;

namespace RCommon.ObjectAccess.EFCore
{
    /// <summary>
    ///     <para>
    ///         Provides a simple API surface for configuring <see cref="DbContextOptions" />. Databases (and other extensions)
    ///         typically define extension methods on this object that allow you to configure the database connection (and other
    ///         options) to be used for a context.
    ///     </para>
    ///     <para>
    ///         You can use <see cref="DataStoreDbContextOptionsBuilder" /> to configure a context by overriding
    ///         <see cref="DbContext.OnConfiguring(DataStoreDbContextOptionsBuilder)" /> or creating a <see cref="DbContextOptions" />
    ///         externally and passing it to the context constructor.
    ///     </para>
    /// </summary>
    public class DataStoreDbContextOptionsBuilder : IDbContextOptionsBuilderInfrastructure
    {
        private DataStoreDbContextOptions _options;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataStoreDbContextOptionsBuilder" /> class with no options set.
        /// </summary>
        public DataStoreDbContextOptionsBuilder()
            : this(new DataStoreDbContextOptions<IDataStore<DbContext>>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataStoreDbContextOptionsBuilder" /> class to further configure
        ///     a given <see cref="DbContextOptions" />.
        /// </summary>
        /// <param name="options"> The options to be configured. </param>
        public DataStoreDbContextOptionsBuilder([NotNull] DataStoreDbContextOptions options)
        {
            Guard.IsNotNull(options, nameof(options));

            _options = options;
        }

        /// <summary>
        ///     Gets the options being configured.
        /// </summary>
        public virtual DataStoreDbContextOptions Options => _options;

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether any options have been configured.
        ///     </para>
        ///     <para>
        ///         This can be useful when you have overridden <see cref="DbContext.OnConfiguring(DataStoreDbContextOptionsBuilder)" /> to configure
        ///         the context, but in some cases you also externally provide options via the context constructor. This property can be
        ///         used to determine if the options have already been set, and skip some or all of the logic in
        ///         <see cref="DbContext.OnConfiguring(DataStoreDbContextOptionsBuilder)" />.
        ///     </para>
        /// </summary>
        public virtual bool IsConfigured => _options.Extensions.Any(e => e.Info.IsDatabaseProvider);

        

        /// <summary>
        ///     <para>
        ///         Sets the <see cref="ILoggerFactory" /> that will be used to create <see cref="ILogger" /> instances
        ///         for logging done by this context.
        ///     </para>
        ///     <para>
        ///         There is no need to call this method when using one of the 'AddDbContext' methods, including 'AddDbContextPool'.
        ///         These methods ensure that the <see cref="ILoggerFactory" /> used by EF is obtained from the application service provider.
        ///     </para>
        ///     <para>
        ///         This method cannot be used if the application is setting the internal service provider
        ///         through a call to <see cref="UseInternalServiceProvider" />. In this case, the <see cref="ILoggerFactory" />
        ///         should be configured directly in that service provider.
        ///     </para>
        /// </summary>
        /// <param name="loggerFactory"> The logger factory to be used. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder UseLoggerFactory([CanBeNull] ILoggerFactory loggerFactory)
            => WithOption(e => e.WithLoggerFactory(loggerFactory));

        

        

        

        

        /// <summary>
        ///     <para>
        ///         Enables detailed errors when handling of data value exceptions that occur during processing of store query results. Such errors
        ///         most often occur due to misconfiguration of entity properties. E.g. If a property is configured to be of type
        ///         'int', but the underlying data in the store is actually of type 'string', then an exception will be generated
        ///         at runtime during processing of the data value. When this option is enabled and a data error is encountered, the
        ///         generated exception will include details of the specific entity property that generated the error.
        ///     </para>
        ///     <para>
        ///         Enabling this option incurs a small performance overhead during query execution.
        ///     </para>
        ///     <para>
        ///         Note that if the application is setting the internal service provider through a call to
        ///         <see cref="UseInternalServiceProvider" />, then this option must configured the same way
        ///         for all uses of that service provider. Consider instead not calling <see cref="UseInternalServiceProvider" />
        ///         so that EF will manage the service providers and can create new instances as required.
        ///     </para>
        /// </summary>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder EnableDetailedErrors(bool detailedErrorsEnabled = true)
            => WithOption(e => e.WithDetailedErrorsEnabled(detailedErrorsEnabled));

        /// <summary>
        ///     <para>
        ///         Sets the <see cref="IMemoryCache" /> to be used for query caching by this context.
        ///     </para>
        ///     <para>
        ///         Note that changing the memory cache can cause EF to build a new internal service provider, which
        ///         may cause issues with performance. Generally it is expected that no more than one or two different
        ///         instances will be used for a given application.
        ///     </para>
        ///     <para>
        ///         This method cannot be used if the application is setting the internal service provider
        ///         through a call to <see cref="UseInternalServiceProvider" />. In this case, the <see cref="IMemoryCache" />
        ///         should be configured directly in that service provider.
        ///     </para>
        /// </summary>
        /// <param name="memoryCache"> The memory cache to be used. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder UseMemoryCache([CanBeNull] IMemoryCache memoryCache)
            => WithOption(e => e.WithMemoryCache(memoryCache));

        /// <summary>
        ///     <para>
        ///         Sets the <see cref="IServiceProvider" /> that the context should resolve all of its services from. EF will
        ///         create and manage a service provider if none is specified.
        ///     </para>
        ///     <para>
        ///         The service provider must contain all the services required by Entity Framework (and the database being
        ///         used). The Entity Framework services can be registered using an extension method on <see cref="IServiceCollection" />.
        ///         For example, the Microsoft SQL Server provider includes an AddEntityFrameworkSqlServer() method to add
        ///         the required services.
        ///     </para>
        ///     <para>
        ///         If the <see cref="IServiceProvider" /> has a <see cref="DbContextOptions" /> or
        ///         <see cref="DbContextOptions{TContext}" /> registered, then this will be used as the options for
        ///         this context instance.
        ///     </para>
        /// </summary>
        /// <param name="serviceProvider"> The service provider to be used. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder UseInternalServiceProvider([CanBeNull] IServiceProvider serviceProvider)
            => WithOption(e => e.WithInternalServiceProvider(serviceProvider));

        /// <summary>
        ///     Sets the <see cref="IServiceProvider" /> from which application services will be obtained. This
        ///     is done automatically when using 'AddDbContext' or 'AddDbContextPool',
        ///     so it is rare that this method needs to be called.
        /// </summary>
        /// <param name="serviceProvider"> The service provider to be used. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder UseApplicationServiceProvider([CanBeNull] IServiceProvider serviceProvider)
            => WithOption(e => e.WithApplicationServiceProvider(serviceProvider));

        /// <summary>
        ///     <para>
        ///         Enables application data to be included in exception messages, logging, etc. This can include the
        ///         values assigned to properties of your entity instances, parameter values for commands being sent
        ///         to the database, and other such data. You should only enable this flag if you have the appropriate
        ///         security measures in place based on the sensitivity of this data.
        ///     </para>
        ///     <para>
        ///         Note that if the application is setting the internal service provider through a call to
        ///         <see cref="UseInternalServiceProvider" />, then this option must configured the same way
        ///         for all uses of that service provider. Consider instead not calling <see cref="UseInternalServiceProvider" />
        ///         so that EF will manage the service providers and can create new instances as required.
        ///     </para>
        /// </summary>
        /// <param name="sensitiveDataLoggingEnabled"> If <see langword="true" />, then sensitive data is logged. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder EnableSensitiveDataLogging(bool sensitiveDataLoggingEnabled = true)
            => WithOption(e => e.WithSensitiveDataLoggingEnabled(sensitiveDataLoggingEnabled));

        /// <summary>
        ///     <para>
        ///         Enables or disables caching of internal service providers. Disabling caching can
        ///         massively impact performance and should only be used in testing scenarios that
        ///         build many service providers for test isolation.
        ///     </para>
        ///     <para>
        ///         Note that if the application is setting the internal service provider through a call to
        ///         <see cref="UseInternalServiceProvider" />, then setting this option wil have no effect.
        ///     </para>
        /// </summary>
        /// <param name="cacheServiceProvider"> If <see langword="true" />, then the internal service provider is cached. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder EnableServiceProviderCaching(bool cacheServiceProvider = true)
            => WithOption(e => e.WithServiceProviderCachingEnabled(cacheServiceProvider));

        /// <summary>
        ///     <para>
        ///         Sets the tracking behavior for LINQ queries run against the context. Disabling change tracking
        ///         is useful for read-only scenarios because it avoids the overhead of setting up change tracking for each
        ///         entity instance. You should not disable change tracking if you want to manipulate entity instances and
        ///         persist those changes to the database using <see cref="DbContext.SaveChanges()" />.
        ///     </para>
        ///     <para>
        ///         This method sets the default behavior for all contexts created with these options, but you can override this
        ///         behavior for a context instance using <see cref="ChangeTracker.QueryTrackingBehavior" /> or on individual
        ///         queries using the <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})" />
        ///         and <see cref="EntityFrameworkQueryableExtensions.AsTracking{TEntity}(IQueryable{TEntity})" /> methods.
        ///     </para>
        ///     <para>
        ///         The default value is <see cref="EntityFrameworkCore.QueryTrackingBehavior.TrackAll" />. This means
        ///         the change tracker will keep track of changes for all entities that are returned from a LINQ query.
        ///     </para>
        /// </summary>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder UseQueryTrackingBehavior(QueryTrackingBehavior queryTrackingBehavior)
            => WithOption(e => e.WithQueryTrackingBehavior(queryTrackingBehavior));

        

        /// <summary>
        ///     <para>
        ///         Replaces all internal Entity Framework implementations of a service contract with a different
        ///         implementation.
        ///     </para>
        ///     <para>
        ///         This method can only be used when EF is building and managing its internal service provider.
        ///         If the service provider is being built externally and passed to
        ///         <see cref="UseInternalServiceProvider" />, then replacement services should be configured on
        ///         that service provider before it is passed to EF.
        ///     </para>
        ///     <para>
        ///         The replacement service gets the same scope as the EF service that it is replacing.
        ///     </para>
        /// </summary>
        /// <typeparam name="TService"> The type (usually an interface) that defines the contract of the service to replace. </typeparam>
        /// <typeparam name="TImplementation"> The new implementation type for the service. </typeparam>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataStoreDbContextOptionsBuilder ReplaceService<TService, TImplementation>()
            where TImplementation : TService
            => WithOption(e => e.WithReplacedService(typeof(TService), typeof(TImplementation)));

        

        

        /// <summary>
        ///     <para>
        ///         Adds the given extension to the options. If an existing extension of the same type already exists, it will be replaced.
        ///     </para>
        ///     <para>
        ///         This method is intended for use by extension methods to configure the context. It is not intended to be used in
        ///         application code.
        ///     </para>
        /// </summary>
        /// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
        /// <param name="extension"> The extension to be added. </param>
        void IDbContextOptionsBuilderInfrastructure.AddOrUpdateExtension<TExtension>(TExtension extension)
        {
            Guard.IsNotNull(extension, nameof(extension));

            _options = _options.WithExtension(extension);
        }

        private DataStoreDbContextOptionsBuilder WithOption(Func<CoreOptionsExtension, CoreOptionsExtension> withFunc)
        {
            ((IDbContextOptionsBuilderInfrastructure)this).AddOrUpdateExtension(
                withFunc(Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension()));

            return this;
        }

        #region Hidden System.Object members

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns> A string that represents the current object. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"> The object to compare with the current object. </param>
        /// <returns> true if the specified object is equal to the current object; otherwise, false. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        ///     Serves as the default hash function.
        /// </summary>
        /// <returns> A hash code for the current object. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        #endregion
    }
}