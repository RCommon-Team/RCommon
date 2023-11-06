using System;
using System.Collections.Generic;
using MediatR;
using RCommon.Extensions;
using PropertyChanged;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace RCommon.BusinessEntities
{
    /// <inheritdoc/>
    [Serializable]
    public abstract class BusinessEntity : IBusinessEntity
    {
        private bool _allowEventTracking = true;
        public BusinessEntity()
        {
            _localEvents = _localEvents ?? new List<ILocalEvent>();
        }

        
        public event EventHandler<LocalEventsChangedEventArgs> LocalEventsAdded;
        public event EventHandler<LocalEventsChangedEventArgs> LocalEventsRemoved;
        public event EventHandler<LocalEventsClearedEventArgs> LocalEventsCleared;


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

        [NotMapped]
        public IReadOnlyCollection<ILocalEvent> LocalEvents => _localEvents?.AsReadOnly();

        [NotMapped]
        public bool AllowEventTracking { get => _allowEventTracking; set => _allowEventTracking = value; }

        public void AddLocalEvent(ILocalEvent eventItem)
        {
            _localEvents.Add(eventItem);
            this.OnLocalEventsAdded(new LocalEventsChangedEventArgs(this, eventItem));
        }

        public void RemoveLocalEvent(ILocalEvent eventItem)
        {
            _localEvents?.Remove(eventItem);
            this.OnLocalEventsRemoved(new LocalEventsChangedEventArgs(this, eventItem));
        }

        public void ClearLocalEvents()
        {
            _localEvents?.Clear();
            this.OnLocalEventsCleared(new LocalEventsClearedEventArgs(this, this.LocalEvents));
        }

        protected void OnLocalEventsAdded(LocalEventsChangedEventArgs args)
        {
            EventHandler<LocalEventsChangedEventArgs> handler = LocalEventsAdded;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected void OnLocalEventsRemoved(LocalEventsChangedEventArgs args)
        {
            EventHandler<LocalEventsChangedEventArgs> handler = LocalEventsRemoved;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected void OnLocalEventsCleared(LocalEventsClearedEventArgs args)
        {
            EventHandler<LocalEventsClearedEventArgs> handler = LocalEventsCleared;
            if (handler != null)
            {
                handler(this, args);
            }
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
