using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices.Sql
{
    public class RDbConnectionOptions
    {
        public RDbConnectionOptions()
        {

        }

        public DbProviderFactory DbFactory { get; set; }
        public string ConnectionString { get; set; }
	}
}
