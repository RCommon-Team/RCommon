using System.Collections.Generic;

namespace RCommon.Entities
{
    /// <summary>
    /// Defines an entity. It's primary key may not be "Id" or it may have a composite primary key.
    /// Use <see cref="IEntity{TKey}"/> where possible for better integration to repositories and other structures in the framework.
    /// </summary>
    public interface IBusinessEntity : ITrackedEntity
    {
        /// <summary>
        /// Returns an array of ordered keys for this entity.
        /// </summary>
        /// <returns></returns>
        object[] GetKeys();

        IReadOnlyCollection<ILocalEvent> LocalEvents { get; }

        void AddLocalEvent(ILocalEvent eventItem);
        void ClearLocalEvents();
        bool EntityEquals(IBusinessEntity other);
        void RemoveLocalEvent(ILocalEvent eventItem);
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
