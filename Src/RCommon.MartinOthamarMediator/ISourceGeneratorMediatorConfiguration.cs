
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mediator;

namespace RCommon.MartinOthamarMediator
{
    public interface ISourceGeneratorMediatorConfiguration : IMediatorConfiguration
    {

        ISourceGeneratorMediatorConfiguration AddSourceGeneratorMediator();
        ISourceGeneratorMediatorConfiguration AddSourceGeneratorMediator(Action<MediatorOptions> options);
    }
}
