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
    public class SalesPersonMap : DommelEntityMap<SalesPerson>
    {

        public SalesPersonMap()
        {
            ToTable("SalesPerson", "dbo");
            Map(x => x.Id).ToColumn("Id", false)
                .IsIdentity()
                .IsKey()
                .SetGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity);
            Map(x => x.Department).Ignore();
            Map(x => x.LocalEvents).Ignore();
            Map(x => x.AllowChangeTracking).Ignore();
        }
    }
}
