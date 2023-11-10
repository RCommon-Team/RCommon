using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediatr
{
    public interface IMediatrConfiguration : IMediatorConfiguration
    {
        IMediatrConfiguration AddMediatr(Action<MediatRServiceConfiguration> options);
        IMediatrConfiguration AddMediatr(MediatRServiceConfiguration options);
    }
}
