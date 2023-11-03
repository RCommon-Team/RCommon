using Dapper.FluentMap.Dommel.Mapping;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper.Tests.Configurations
{
    public class OrderMap : DommelEntityMap<Order>
    {

        public OrderMap()
        {
            ToTable("Orders", "dbo");
            Map(x => x.Id).ToColumn("OrderID", false)
                .IsIdentity()
                .IsKey()
                .SetGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity);
            Map(x => x.Customer).Ignore();
            Map(x => x.LocalEvents).Ignore();
            Map(x => x.AllowEventTracking).Ignore();
        }
    }
}
