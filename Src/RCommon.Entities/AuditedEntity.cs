using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base class for entities that track creation and modification audit information.
    /// Uses <see cref="BusinessEntity"/> as the base (no explicit primary key type).
    /// </summary>
    /// <typeparam name="TCreatedByUser">The type representing the user who created the entity.</typeparam>
    /// <typeparam name="TLastModifiedByUser">The type representing the user who last modified the entity.</typeparam>
    /// <seealso cref="IAuditedEntity{TCreatedByUser, TLastModifiedByUser}"/>
    /// <seealso cref="BusinessEntity"/>
    public abstract class AuditedEntity<TCreatedByUser, TLastModifiedByUser> : BusinessEntity, IAuditedEntity<TCreatedByUser, TLastModifiedByUser>
    {
        /// <inheritdoc />
        public DateTime? DateCreated { get; set; }

        /// <inheritdoc />
        public TCreatedByUser? CreatedBy { get; set; }

        /// <inheritdoc />
        public DateTime? DateLastModified { get; set; }

        /// <inheritdoc />
        public TLastModifiedByUser? LastModifiedBy { get; set; }
    }

    /// <summary>
    /// Abstract base class for entities that track creation and modification audit information
    /// with an explicit strongly-typed primary key.
    /// </summary>
    /// <typeparam name="TKey">The type of the entity's primary key. Must implement <see cref="IEquatable{TKey}"/>.</typeparam>
    /// <typeparam name="TCreatedByUser">The type representing the user who created the entity.</typeparam>
    /// <typeparam name="TLastModifiedByUser">The type representing the user who last modified the entity.</typeparam>
    /// <seealso cref="BusinessEntity{TKey}"/>
    /// <seealso cref="IAuditedEntity{TKey, TCreatedByUser, TLastModifiedByUser}"/>
    public abstract class AuditedEntity<TKey, TCreatedByUser, TLastModifiedByUser>
        : BusinessEntity<TKey>, IAuditedEntity<TCreatedByUser, TLastModifiedByUser>, IAuditedEntity<TKey, TCreatedByUser, TLastModifiedByUser>
        where TKey : IEquatable<TKey>
    {
        /// <inheritdoc />
        public DateTime? DateCreated { get; set; }

        /// <inheritdoc />
        public TCreatedByUser? CreatedBy { get; set; }

        /// <inheritdoc />
        public DateTime? DateLastModified { get; set; }

        /// <inheritdoc />
        public TLastModifiedByUser? LastModifiedBy { get; set; }
    }
}
