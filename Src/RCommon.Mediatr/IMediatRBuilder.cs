using Microsoft.Extensions.DependencyInjection;

namespace RCommon.Mediator.MediatR
{
    public interface IMediatRBuilder : IMediatorBuilder
    {
        IMediatRBuilder Configure(Action<MediatRServiceConfiguration> options);
        IMediatRBuilder Configure(MediatRServiceConfiguration options);
    }
}
