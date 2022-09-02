using Dapper.FluentMap.Dommel.Mapping;
using Dapper.FluentMap.Mapping;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper.Tests.Configurations
{
    public class CustomerMap : DommelEntityMap<Customer>
    {

        public CustomerMap()
        {
            ToTable("Customers");
            Map(x => x.Id).ToColumn("CustomerId", false)
                .IsIdentity()
                .IsKey()
                .SetGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity);
            Map(x => x.Orders).Ignore();
            Map(x => x.LocalEvents).Ignore();
            Map(x => x.AllowChangeTracking).Ignore();
        }
    }
}
