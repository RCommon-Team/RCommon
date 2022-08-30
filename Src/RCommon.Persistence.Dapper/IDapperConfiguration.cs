using DapperExtensions.Mapper;
using RCommon.Configuration;
using RCommon.DataServices.Sql;
using System;

namespace RCommon
{
    public interface IDapperConfiguration : IServiceConfiguration
    {
        IDapperConfiguration UsingDbConnection<TDbConnection>() where TDbConnection : IRDbConnection;

        IDapperConfiguration WithPluralizedClassMapper();

        IDapperConfiguration WithSingularizedClassMapper();
        IDapperConfiguration WithCustomClassMapper(Type type);
    }
}
