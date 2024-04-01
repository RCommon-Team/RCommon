﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestResponse
    {
        public TestResponse(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
