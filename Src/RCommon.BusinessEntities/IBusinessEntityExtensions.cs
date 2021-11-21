using MediatR;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public static class IBusinessEntityExtensions
    {
        public static void PublishLocalEvents(this IBusinessEntity entity, IMediator mediator)
        {
            var relatedEntities = entity.TraverseGraphFor<IBusinessEntity>();
            relatedEntities.ForEach(relatedEntity =>
                relatedEntity.LocalEvents.ForEach(localEvent =>
                    mediator.Publish(localEvent)));
        }
    }
}
