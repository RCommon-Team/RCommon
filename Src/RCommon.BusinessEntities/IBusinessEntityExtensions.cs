using MediatR;
using Microsoft.Extensions.Logging;
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
        public static void PublishLocalEvents(this IBusinessEntity entity, IMediator mediator, ILogger? logger = null)
        {
            var relatedEntities = entity.TraverseGraphFor<IBusinessEntity>();
            relatedEntities.ForEach(relatedEntity =>
                relatedEntity.LocalEvents.ForEach(localEvent =>
                {
                    if (logger != null)
                    {
                        var eventName = localEvent.GetGenericTypeName();
                        var entityName = entity.GetGenericTypeName();
                        logger.LogInformation("----- Publishing local event: {EventName} for entity: {EntityName}", entityName, entityName);
                    }
                    mediator.Publish(localEvent);
                }));
                    
        }
    }
}
