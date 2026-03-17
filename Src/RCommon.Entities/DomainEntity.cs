using System;

namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base class for domain entities within an aggregate. Provides identity-based equality
    /// but no event tracking — entities within an aggregate raise events through their aggregate root.
    /// Because DomainEntity does not implement IBusinessEntity, the ObjectGraphWalker in
    /// InMemoryEntityEventTracker will not traverse it. All domain events must be raised on the
    /// aggregate root.
    /// </summary>
    /// <typeparam name="TKey">The type of the entity's identity.</typeparam>
    [Serializable]
    public abstract class DomainEntity<TKey> : IEquatable<DomainEntity<TKey>>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// The unique identity of this entity.
        /// </summary>
        public virtual TKey Id { get; protected set; } = default!;

        public bool Equals(DomainEntity<TKey>? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (IsTransient() || other.IsTransient())
                return false;

            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
            => Equals(obj as DomainEntity<TKey>);

        public override int GetHashCode()
        {
            var id = Id;
            if (id is null || id.Equals(default(TKey)))
                return base.GetHashCode();
            return id.GetHashCode();
        }

        /// <summary>
        /// Returns true if this entity has not yet been assigned a persistent identity.
        /// </summary>
        public bool IsTransient()
            => Id is null || Id.Equals(default);

        public static bool operator ==(DomainEntity<TKey>? left, DomainEntity<TKey>? right)
        {
            if (left is null && right is null)
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(DomainEntity<TKey>? left, DomainEntity<TKey>? right)
            => !(left == right);
    }
}
