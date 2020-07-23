// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Utilities;
using RCommon.DataServices;

namespace RCommon.ObjectAccess.EFCore
{
    /// <summary>
    ///     The options to be used by a <see cref="DbContext" />. You normally override
    ///     <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> or use a <see cref="DbContextOptionsBuilder{TContext}" />
    ///     to create instances of this class and it is not designed to be directly constructed in your application code.
    /// </summary>
    /// <typeparam name="TContext"> The type of the context these options apply to. </typeparam>
    public class DataStoreDbContextOptions<TContext> : DataStoreDbContextOptions
        where TContext : RCommonDbContext
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbContextOptions{TContext}" /> class. You normally override
        ///     <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> or use a <see cref="DbContextOptionsBuilder{TContext}" />
        ///     to create instances of this class and it is not designed to be directly constructed in your application code.
        /// </summary>
        public DataStoreDbContextOptions()
            : base(new Dictionary<Type, IDbContextOptionsExtension>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbContextOptions{TContext}" /> class. You normally override
        ///     <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> or use a <see cref="DbContextOptionsBuilder{TContext}" />
        ///     to create instances of this class and it is not designed to be directly constructed in your application code.
        /// </summary>
        /// <param name="extensions"> The extensions that store the configured options. </param>
        public DataStoreDbContextOptions(
            [NotNull] IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions)
            : base(extensions)
        {
        }

        /// <summary>
        ///     Adds the given extension to the underlying options and creates a new
        ///     <see cref="DbContextOptions" /> with the extension added.
        /// </summary>
        /// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
        /// <param name="extension"> The extension to be added. </param>
        /// <returns> The new options instance with the given extension added. </returns>
        public override DataStoreDbContextOptions WithExtension<TExtension>(TExtension extension)
        {
            Guard.IsNotNull(extension, nameof(extension));

            var extensions = Extensions.ToDictionary(p => p.GetType(), p => p);
            extensions[typeof(TExtension)] = extension;

            return new DataStoreDbContextOptions<TContext>(extensions);
        }

        /// <summary>
        ///     The type of context that these options are for (<typeparamref name="TContext" />).
        /// </summary>
        public override Type ContextType => typeof(TContext);
    }
}