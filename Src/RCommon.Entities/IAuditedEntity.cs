using System;

namespace RCommon.Entities
{
    /// <summary>
    /// Defines the contract for an entity that captures audit information including
    /// who created and last modified it, and when.
    /// </summary>
    /// <typeparam name="TCreatedByUser">The type representing the user who created the entity.</typeparam>
    /// <typeparam name="TLastModifiedByUser">The type representing the user who last modified the entity.</typeparam>
    /// <seealso cref="IBusinessEntity"/>
    public interface IAuditedEntity<TCreatedByUser, TLastModifiedByUser>
        : IBusinessEntity
    {
        /// <summary>
        /// Gets or sets the user who created this entity.
        /// </summary>
        TCreatedByUser? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this entity was created.
        /// </summary>
        DateTime? DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this entity was last modified.
        /// </summary>
        DateTime? DateLastModified { get; set; }

        /// <summary>
        /// Gets or sets the user who last modified this entity.
        /// </summary>
        TLastModifiedByUser? LastModifiedBy { get; set; }
    }

    /// <summary>
    /// Extends <see cref="IAuditedEntity{TCreatedByUser, TLastModifiedByUser}"/> with a strongly-typed primary key.
    /// </summary>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    /// <typeparam name="TCreatedByUser">The type representing the user who created the entity.</typeparam>
    /// <typeparam name="TLastModifiedByUser">The type representing the user who last modified the entity.</typeparam>
    /// <seealso cref="IBusinessEntity{TKey}"/>
    public interface IAuditedEntity<TKey, TCreatedByUser, TLastModifiedByUser>
        : IAuditedEntity<TCreatedByUser, TLastModifiedByUser>, IBusinessEntity<TKey>
    {

    }
}
