﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    internal static class ConfigurationContainer
    {
        public static IConfiguration Configuration { get; set; }
    }
}
