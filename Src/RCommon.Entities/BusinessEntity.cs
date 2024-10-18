using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using RCommon.EventHandling;

namespace RCommon.Entities
{
    /// <inheritdoc/>
    [Serializable]
    public abstract class BusinessEntity : IBusinessEntity
    {
        private bool _allowEventTracking = true;
        private List<ISerializableEvent> _localEvents;

        public BusinessEntity()
        {
            _localEvents = _localEvents ?? new List<ISerializableEvent>();
        }

        
        public event EventHandler<TransactionalEventsChangedEventArgs> TransactionalEventAdded;
        public event EventHandler<TransactionalEventsChangedEventArgs> TransactionalEventRemoved;
        public event EventHandler<TransactionalEventsClearedEventArgs> TransactionalEventsCleared;


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

        [NotMapped]
        public IReadOnlyCollection<ISerializableEvent> LocalEvents => _localEvents?.AsReadOnly();

        [NotMapped]
        public bool AllowEventTracking { get => _allowEventTracking; set => _allowEventTracking = value; }

        public void AddLocalEvent(ISerializableEvent eventItem)
        {
            _localEvents.Add(eventItem);
            this.OnLocalEventsAdded(new TransactionalEventsChangedEventArgs(this, eventItem));
        }

        public void RemoveLocalEvent(ISerializableEvent eventItem)
        {
            _localEvents?.Remove(eventItem);
            this.OnLocalEventsRemoved(new TransactionalEventsChangedEventArgs(this, eventItem));
        }

        public void ClearLocalEvents()
        {
            _localEvents?.Clear();
            this.OnLocalEventsCleared(new TransactionalEventsClearedEventArgs(this));
        }

        protected void OnLocalEventsAdded(TransactionalEventsChangedEventArgs args)
        {
            EventHandler<TransactionalEventsChangedEventArgs> handler = TransactionalEventAdded;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected void OnLocalEventsRemoved(TransactionalEventsChangedEventArgs args)
        {
            EventHandler<TransactionalEventsChangedEventArgs> handler = TransactionalEventRemoved;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected void OnLocalEventsCleared(TransactionalEventsClearedEventArgs args)
        {
            EventHandler<TransactionalEventsClearedEventArgs> handler = TransactionalEventsCleared;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }

    /// <inheritdoc cref="IBusinessEntity{TKey}" />
    [Serializable]
    public abstract class BusinessEntity<TKey> : BusinessEntity, IBusinessEntity<TKey>
        where TKey : IEquatable<TKey>
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
