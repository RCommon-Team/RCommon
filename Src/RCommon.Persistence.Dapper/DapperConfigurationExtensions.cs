using Dapper.FluentMap;
using Dapper.FluentMap.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.Persistence;
using RCommon.Persistence.Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class DapperConfigurationExtensions
    {

        public static IDapperConfiguration AddFluentMappings(this IDapperConfiguration config,
            Action<FluentMapConfiguration> fluentMapConfig)
        {
            Guard.Against<DapperFluentMappingsException>(fluentMapConfig == null, "You must configure the fluent mappings options for fluent mappings to be useful");
            FluentMapper.Initialize(fluentMapConfig);

            return config;
        }
    }
}
