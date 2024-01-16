using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon
{
    public interface IRCommonBuilder
    {
        IServiceCollection Services { get; }

        IServiceCollection Configure();
        IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions);
        IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions);
        IRCommonBuilder WithSimpleGuidGenerator();
        IRCommonBuilder WithCommonFactory<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;
    }
}
