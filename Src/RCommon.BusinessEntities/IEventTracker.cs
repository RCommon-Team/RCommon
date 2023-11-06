using System.Collections.Generic;

namespace RCommon.BusinessEntities
{
    public interface IEventTracker
    {
        ICollection<IBusinessEntity> TrackedEntities { get; }

        void AddEntity(IBusinessEntity entity);
    }
}