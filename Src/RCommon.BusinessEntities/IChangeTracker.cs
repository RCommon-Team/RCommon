using System.Collections.Generic;

namespace RCommon.BusinessEntities
{
    public interface IChangeTracker
    {
        ICollection<IBusinessEntity> TrackedEntities { get; }

        void AddEntity(IBusinessEntity entity);
    }
}