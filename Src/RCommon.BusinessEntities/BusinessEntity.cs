using System;
using System.Collections.Generic;
using MediatR;
using RCommon.Extensions;

namespace RCommon.BusinessEntities
{
    /// <inheritdoc/>
    [Serializable]
    public abstract class BusinessEntity : IBusinessEntity
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[ENTITY: {GetType().Name}] Keys = {GetKeys().GetDelimitedString(',')}";
        }

        public abstract object[] GetKeys();

        public bool EntityEquals(IBusinessEntity other)
        {
            return this.BinaryEquals(other);
        }

        private List<ILocalEvent> _localEvents;
        public IReadOnlyCollection<ILocalEvent> LocalEvents => _localEvents?.AsReadOnly();

        public void AddLocalEvent(ILocalEvent eventItem)
        {
            _localEvents = _localEvents ?? new List<ILocalEvent>();
            _localEvents.Add(eventItem);
        }

        public void RemoveLocalEvent(ILocalEvent eventItem)
        {
            _localEvents?.Remove(eventItem);
        }

        public void ClearLocalEvents()
        {
            _localEvents?.Clear();
        }
    }

    /// <inheritdoc cref="IBusinessEntity{TKey}" />
    [Serializable]
    public abstract class BusinessEntity<TKey> : BusinessEntity, IBusinessEntity<TKey>
    {
        /// <inheritdoc/>
        public virtual TKey Id { get; protected set; }

        protected BusinessEntity()
        {

        }

        protected BusinessEntity(TKey id)
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
