using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Json.SystemTextJson
{
    public class TestDto
    {
        public TestDto(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
    }
}
