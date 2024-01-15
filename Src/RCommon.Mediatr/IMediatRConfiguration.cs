using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public interface IMediatrConfiguration : IMediatorBuilder
    {
        IMediatrConfiguration AddMediatr(Action<MediatRServiceConfiguration> options);
        IMediatrConfiguration AddMediatr(MediatRServiceConfiguration options);
    }
}
