using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.Persistence.Sql;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Persistence.Dapper.Tests
{
    public class TestDbConnection : RDbConnection
    {

        public TestDbConnection(IOptions<RDbConnectionOptions> options, ILocalEventTracker eventTracker) 
            : base(options, eventTracker)
        {
            
        }
    }
}
