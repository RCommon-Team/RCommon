using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Persistence.Configurations.Entities
{
    public static class BaseConfiguration
    {
        public static void Configure<T>(EntityTypeBuilder<T> entity)
            where T : BusinessEntity
        {

            entity.Ignore(x => x.AllowChangeTracking);
            //entity.Ignore(x => x.IsChanged);
            entity.Ignore(x => x.LocalEvents);



        }
    }
}
