using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using RCommon.EventHandling;
using RCommon.Models.Events;

namespace RCommon.Entities
{
    /// <inheritdoc/>
    /// <remarks>
    /// Provides a base implementation for domain entities with support for transactional (local)
    /// event tracking. Local events are accumulated on the entity and can be emitted via an
    /// <see cref="IEntityEventTracker"/> during persistence operations.
    /// </remarks>
    [Serializable]
    public abstract class BusinessEntity : IBusinessEntity
    {
        private bool _allowEventTracking = true;
        private List<ISerializableEvent> _localEvents = new List<ISerializableEvent>();

        /// <summary>
        /// Initializes a new instance of <see cref="BusinessEntity"/>.
        /// </summary>
        public BusinessEntity()
        {
            // Ensure the local events list is initialized (null-coalesce guards against serialization scenarios)
            _localEvents = _localEvents ?? new List<ISerializableEvent>();
        }


        /// <summary>
        /// Occurs when a transactional event is added to this entity.
        /// </summary>
        public event EventHandler<TransactionalEventsChangedEventArgs>? TransactionalEventAdded;

        /// <summary>
        /// Occurs when a transactional event is removed from this entity.
        /// </summary>
        public event EventHandler<TransactionalEventsChangedEventArgs>? TransactionalEventRemoved;

        /// <summary>
        /// Occurs when all transactional events are cleared from this entity.
        /// </summary>
        public event EventHandler<TransactionalEventsClearedEventArgs>? TransactionalEventsCleared;


        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[ENTITY: {GetType().Name}] Keys = {GetKeys().GetDelimitedString(',')}";
        }

        /// <inheritdoc />
        public abstract object[] GetKeys();

        /// <inheritdoc />
        public bool EntityEquals(IBusinessEntity other)
        {
            return this.BinaryEquals(other);
        }

        /// <inheritdoc />
        [NotMapped]
        public IReadOnlyCollection<ISerializableEvent> LocalEvents => _localEvents.AsReadOnly();

        /// <inheritdoc />
        [NotMapped]
        public bool AllowEventTracking { get => _allowEventTracking; set => _allowEventTracking = value; }

        /// <inheritdoc />
        public void AddLocalEvent(ISerializableEvent eventItem)
        {
            _localEvents.Add(eventItem);
            this.OnLocalEventsAdded(new TransactionalEventsChangedEventArgs(this, eventItem));
        }

        /// <inheritdoc />
        public void RemoveLocalEvent(ISerializableEvent eventItem)
        {
            _localEvents?.Remove(eventItem);
            this.OnLocalEventsRemoved(new TransactionalEventsChangedEventArgs(this, eventItem));
        }

        /// <inheritdoc />
        public void ClearLocalEvents()
        {
            _localEvents?.Clear();
            this.OnLocalEventsCleared(new TransactionalEventsClearedEventArgs(this));
        }

        /// <summary>
        /// Raises the <see cref="TransactionalEventAdded"/> event.
        /// </summary>
        /// <param name="args">The event arguments containing the entity and the added event.</param>
        protected void OnLocalEventsAdded(TransactionalEventsChangedEventArgs args)
        {
            // Capture delegate to a local variable for thread safety
            EventHandler<TransactionalEventsChangedEventArgs>? handler = TransactionalEventAdded;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Raises the <see cref="TransactionalEventRemoved"/> event.
        /// </summary>
        /// <param name="args">The event arguments containing the entity and the removed event.</param>
        protected void OnLocalEventsRemoved(TransactionalEventsChangedEventArgs args)
        {
            // Capture delegate to a local variable for thread safety
            EventHandler<TransactionalEventsChangedEventArgs>? handler = TransactionalEventRemoved;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Raises the <see cref="TransactionalEventsCleared"/> event.
        /// </summary>
        /// <param name="args">The event arguments containing the entity whose events were cleared.</param>
        protected void OnLocalEventsCleared(TransactionalEventsClearedEventArgs args)
        {
            // Capture delegate to a local variable for thread safety
            EventHandler<TransactionalEventsClearedEventArgs>? handler = TransactionalEventsCleared;
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
        public virtual TKey Id { get; protected set; } = default!;

        /// <summary>
        /// Initializes a new instance of <see cref="BusinessEntity{TKey}"/> with a default key.
        /// </summary>
        protected BusinessEntity()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="BusinessEntity{TKey}"/> with the specified key.
        /// </summary>
        /// <param name="id">The primary key value for this entity.</param>
        protected BusinessEntity(TKey id)
        {
            Id = id;
        }

        /// <inheritdoc />
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
