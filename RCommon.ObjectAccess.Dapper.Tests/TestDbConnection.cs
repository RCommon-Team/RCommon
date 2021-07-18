using Microsoft.Extensions.Configuration;
using RCommon.DataServices.Sql;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ObjectAccess.Dapper.Tests
{
    public class TestDbConnection : RDbConnection
    {

        public TestDbConnection(IConfiguration configuration) : base("System.Data.SqlClient", configuration.GetConnectionString(@"TestDbConnection"))
        {

        }
    }
}
