using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    public interface IJsonBuilder
    {
        IServiceCollection Services { get; }
    }
}
