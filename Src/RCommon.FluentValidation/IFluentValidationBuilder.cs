using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices;

namespace RCommon.FluentValidation
{
    public interface IFluentValidationBuilder : IValidationBuilder
    {
        IServiceCollection Services { get; }
    }
}
