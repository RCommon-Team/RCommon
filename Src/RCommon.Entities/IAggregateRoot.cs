using System;
using System.Collections.Generic;

namespace RCommon.Entities
{
    /// <summary>
    /// Non-generic marker interface for aggregate roots.
    /// Useful for infrastructure scenarios such as repository filtering, middleware, and generic constraints.
    /// </summary>
    public interface IAggregateRoot : IBusinessEntity
    {
        /// <summary>
        /// The version number used for optimistic concurrency control.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// The collection of domain events raised by this aggregate that have not yet been dispatched.
        /// </summary>
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    }

    /// <summary>
    /// Generic interface for aggregate roots in the domain model.
    /// Extends IBusinessEntity to maintain compatibility with existing repository and event tracking infrastructure.
    /// Note: The IEquatable constraint is stricter than IBusinessEntity&lt;TKey&gt; — this is intentional
    /// because aggregate roots require identity equality for consistency guarantees.
    /// </summary>
    public interface IAggregateRoot<TKey> : IAggregateRoot, IBusinessEntity<TKey>
        where TKey : IEquatable<TKey>
    {
    }
}
