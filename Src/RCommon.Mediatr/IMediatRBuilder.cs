using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public interface IMediatRBuilder : IMediatorBuilder
    {
        IMediatRBuilder AddMediatr(Action<MediatRServiceConfiguration> options);
        IMediatRBuilder AddMediatr(MediatRServiceConfiguration options);
    }
}
