using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public static class BusinessEntityCollectionExtensions
    {

        public static void PublishLocalEvents(this IEnumerable<IBusinessEntity> entities, IMediator mediator, ILogger? logger = null)
        {
            foreach (var item in entities)
            {
                item.PublishLocalEvents(mediator, logger);
            }
        }
    }
}
