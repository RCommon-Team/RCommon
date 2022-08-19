using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public abstract class AuditedEntity<TCreatedByUser, TLastModifiedByUser> : BusinessEntity, IAuditedEntity<TCreatedByUser, TLastModifiedByUser>
    {
        public DateTime? DateCreated { get; set; }
        public TCreatedByUser? CreatedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public TLastModifiedByUser? LastModifiedBy { get; set; }
    }

    public abstract class AuditedEntity<TKey, TCreatedByUser, TLastModifiedByUser>
        : BusinessEntity<TKey>, IAuditedEntity<TCreatedByUser, TLastModifiedByUser>, IAuditedEntity<TKey, TCreatedByUser, TLastModifiedByUser>
    {
        public DateTime? DateCreated { get; set; }
        public TCreatedByUser? CreatedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public TLastModifiedByUser? LastModifiedBy { get; set; }
    }
}
