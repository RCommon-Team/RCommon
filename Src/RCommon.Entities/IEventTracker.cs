using System.Collections.Generic;

namespace RCommon.Entities
{
    public interface IEventTracker
    {
        ICollection<IBusinessEntity> TrackedEntities { get; }

        void AddEntity(IBusinessEntity entity);
    }
}