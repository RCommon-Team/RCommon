using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon
{
    public interface IRCommonConfiguration
    {
        IServiceCollection Services { get; }

        IServiceCollection Configure();
        IRCommonConfiguration WithDateTimeSystem(Action<SystemTimeOptions> actions);
        IRCommonConfiguration WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions);
        IRCommonConfiguration WithSimpleGuidGenerator();
    }
}
