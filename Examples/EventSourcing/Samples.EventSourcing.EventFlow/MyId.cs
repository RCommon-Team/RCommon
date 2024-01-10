using EventFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.EventSourcing.EventFlow
{
    public class MyId : Identity<MyId>
    {
        public MyId(string value) : base(value) { }
    }
}
