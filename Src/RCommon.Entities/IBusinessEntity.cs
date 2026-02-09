using RCommon.EventHandling;
using RCommon.Models.Events;
using System.Collections.Generic;

namespace RCommon.Entities
{
    /// <summary>
    /// Defines an entity. It's primary key may not be "Id" or it may have a composite primary key.
    /// Use <see cref="IBusinessEntity{TKey}"/> where possible for better integration to repositories and other structures in the framework.
    /// </summary>
    public interface IBusinessEntity : ITrackedEntity
    {
        /// <summary>
        /// Returns an array of ordered keys for this entity.
        /// </summary>
        /// <returns>An object array containing the entity's key values in order.</returns>
        object[] GetKeys();

        /// <summary>
        /// Gets the read-only collection of local (transactional) events accumulated on this entity.
        /// </summary>
        IReadOnlyCollection<ISerializableEvent> LocalEvents { get; }

        /// <summary>
        /// Adds a transactional event to this entity's local event collection.
        /// </summary>
        /// <param name="eventItem">The serializable event to add.</param>
        void AddLocalEvent(ISerializableEvent eventItem);

        /// <summary>
        /// Removes all transactional events from this entity's local event collection.
        /// </summary>
        void ClearLocalEvents();

        /// <summary>
        /// Determines whether this entity is equal to another <see cref="IBusinessEntity"/> using binary comparison.
        /// </summary>
        /// <param name="other">The other entity to compare against.</param>
        /// <returns><c>true</c> if the entities are equal; otherwise, <c>false</c>.</returns>
        bool EntityEquals(IBusinessEntity other);

        /// <summary>
        /// Removes a specific transactional event from this entity's local event collection.
        /// </summary>
        /// <param name="eventItem">The serializable event to remove.</param>
        void RemoveLocalEvent(ISerializableEvent eventItem);
    }

    /// <summary>
    /// Defines an entity with a single primary key with "Id" property.
    /// </summary>
    /// <typeparam name="TKey">Type of the primary key of the entity</typeparam>
    public interface IBusinessEntity<TKey> : IBusinessEntity
    {
        /// <summary>
        /// Unique identifier for this entity.
        /// </summary>
        TKey Id { get; }
    }
}
