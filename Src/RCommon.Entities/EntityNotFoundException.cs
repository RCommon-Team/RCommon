
using System;

namespace RCommon.Entities
{
    /// <summary>
    /// Exception thrown when an entity expected to be found does not exist.
    /// </summary>
    /// <seealso cref="GeneralException"/>
    public class EntityNotFoundException : GeneralException
    {
        /// <summary>
        /// Type of the entity.
        /// </summary>
        public Type? EntityType { get; set; }

        /// <summary>
        /// Id of the Entity.
        /// </summary>
        public object? Id { get; set; }

        /// <summary>
        /// Creates a new <see cref="EntityNotFoundException"/> object.
        /// </summary>
        public EntityNotFoundException()
        {

        }

        /// <summary>
        /// Creates a new <see cref="EntityNotFoundException"/> object.
        /// </summary>
        /// <param name="entityType">The type of the entity that was not found.</param>
        public EntityNotFoundException(Type entityType)
            : this(entityType, null, null)
        {

        }

        /// <summary>
        /// Creates a new <see cref="EntityNotFoundException"/> object.
        /// </summary>
        /// <param name="entityType">The type of the entity that was not found.</param>
        /// <param name="id">The identifier of the entity that was not found.</param>
        public EntityNotFoundException(Type entityType, object? id)
            : this(entityType, id, null)
        {

        }

        /// <summary>
        /// Creates a new <see cref="EntityNotFoundException"/> object with an auto-generated message.
        /// </summary>
        /// <param name="entityType">The type of the entity that was not found.</param>
        /// <param name="id">The identifier of the entity that was not found, or <c>null</c> if unknown.</param>
        /// <param name="innerException">The inner exception, or <c>null</c> if none.</param>
        public EntityNotFoundException(Type entityType, object? id, Exception? innerException)
            : base(
                id == null
                    ? $"There is no such an entity given given id. Entity type: {entityType.FullName}"
                    : $"There is no such an entity. Entity type: {entityType.FullName}, id: {id}",
                innerException!)
        {
            EntityType = entityType;
            Id = id;
        }

        /// <summary>
        /// Creates a new <see cref="EntityNotFoundException"/> object.
        /// </summary>
        /// <param name="message">Exception message</param>
        public EntityNotFoundException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Creates a new <see cref="EntityNotFoundException"/> object.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public EntityNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
