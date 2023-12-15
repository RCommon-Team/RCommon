using System.Collections.Generic;

namespace RCommon.Entities
{
    public interface ILocalEventTracker
    {
        ICollection<IBusinessEntity> TrackedEntities { get; }
        void AddEntity(IBusinessEntity entity);
        bool PublishLocalEvents();
    }
}
