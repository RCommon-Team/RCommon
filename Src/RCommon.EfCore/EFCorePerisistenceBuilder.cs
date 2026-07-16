using System;
using Microsoft.Extensions.DependencyInjection;

namespace RCommon
{
    /// <summary>
    /// Obsolete, misspelled alias for <see cref="EFCorePersistenceBuilder"/>, kept as a thin
    /// forwarding subclass so existing consumer code (e.g. <c>WithPersistence&lt;EFCorePerisistenceBuilder&gt;(...)</c>)
    /// keeps compiling. New code should reference <see cref="EFCorePersistenceBuilder"/> directly.
    /// </summary>
    [Obsolete("Use EFCorePersistenceBuilder instead. This name will be removed in a future major version.")]
    public class EFCorePerisistenceBuilder : EFCorePersistenceBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EFCorePerisistenceBuilder"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
        public EFCorePerisistenceBuilder(IServiceCollection services) : base(services)
        {
        }
    }
}
