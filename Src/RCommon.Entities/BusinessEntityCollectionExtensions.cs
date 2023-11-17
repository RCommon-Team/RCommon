using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public static class BusinessEntityCollectionExtensions
    {

        public static void PublishLocalEvents(this IEnumerable<IBusinessEntity> entities, IMediatorService mediator)
        {
            foreach (var item in entities)
            {
                item.PublishLocalEvents(mediator);
            }
        }
    }
}
