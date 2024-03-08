using Microsoft.Extensions.DependencyInjection;

namespace RCommon.ApplicationServices
{
    public interface ICqrsBuilder
    {
        IServiceCollection Services { get; }
    }
}