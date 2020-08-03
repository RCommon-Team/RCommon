using System;
using System.Collections.Generic;
using RCommon.Extensions;

namespace RCommon.Domain.Entities
{
    /// <inheritdoc/>
    [Serializable]
    public abstract class DomainEntity : IDomainEntity
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[ENTITY: {GetType().Name}] Keys = {GetKeys().GetDelimitedString(',')}";
        }

        public abstract object[] GetKeys();

        public bool EntityEquals(IDomainEntity other)
        {
            return this.BinaryEquals(other);
        }
    }

    /// <inheritdoc cref="IEntity{TKey}" />
    [Serializable]
    public abstract class Entity<TKey> : DomainEntity, IDomainEntity<TKey>
    {
        /// <inheritdoc/>
        public virtual TKey Id { get; protected set; }

        protected Entity()
        {

        }

        protected Entity(TKey id)
        {
            Id = id;
        }

        public override object[] GetKeys()
        {
            return new object[] { Id };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[ENTITY: {GetType().Name}] Id = {Id}";
        }
    }
}
