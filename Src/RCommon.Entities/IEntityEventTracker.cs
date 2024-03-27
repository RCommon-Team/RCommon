using RCommon.Entities;
using System.Collections.Generic;

namespace RCommon.Entities
{
    public interface IEntityEventTracker
    {
        ICollection<IBusinessEntity> TrackedEntities { get; }
        void AddEntity(IBusinessEntity entity);
        bool PublishLocalEvents();
    }
}
