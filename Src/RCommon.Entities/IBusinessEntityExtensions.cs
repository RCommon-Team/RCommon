using RCommon.Extensions;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public static class IBusinessEntityExtensions
    {
        public static void PublishLocalEvents(this IBusinessEntity entity, IMediatorService mediator)
        {
            var relatedEntities = entity.TraverseGraphFor<IBusinessEntity>();
            relatedEntities.ForEach(relatedEntity =>
                relatedEntity.LocalEvents.ForEach(localEvent =>
                    mediator.Publish(localEvent)));
        }
    }
}
