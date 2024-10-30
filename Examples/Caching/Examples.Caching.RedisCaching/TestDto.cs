using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Caching.RedisCaching
{
    public record TestDto
    {
        public TestDto(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
