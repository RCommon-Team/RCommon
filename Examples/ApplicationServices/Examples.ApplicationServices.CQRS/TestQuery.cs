using RCommon.Models.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.ApplicationServices.CQRS
{
    public class TestQuery : IQuery<TestDto>
    {
        public TestQuery()
        {
                
        }
    }
}
