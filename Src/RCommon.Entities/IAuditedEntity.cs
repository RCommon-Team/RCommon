using System;

namespace RCommon.Entities
{
    public interface IAuditedEntity<TCreatedByUser, TLastModifiedByUser> 
        : IBusinessEntity
    {
        TCreatedByUser? CreatedBy { get; set; }
        DateTime? DateCreated { get; set; }
        DateTime? DateLastModified { get; set; }
        TLastModifiedByUser? LastModifiedBy { get; set; }
    }

    public interface IAuditedEntity<TKey, TCreatedByUser, TLastModifiedByUser> 
        : IAuditedEntity<TCreatedByUser, TLastModifiedByUser>, IBusinessEntity<TKey>
    {
        
    }
}
