using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mediator;
using RCommon.Mediator;

namespace RCommon.MartinOthamarMediator
{
    public class SourceGeneratorMediatorService : IMediatorService
    {
        private readonly IMediator _sourceGeneratorMediator;

        public SourceGeneratorMediatorService(IMediator sourceGeneratorMediator)
        {
            _sourceGeneratorMediator = sourceGeneratorMediator ?? throw new ArgumentNullException(nameof(sourceGeneratorMediator));
        }

        public async Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            await _sourceGeneratorMediator.Publish(notification, cancellationToken);
        }

        public async Task Send(object notification, CancellationToken cancellationToken = default)
        {
            await _sourceGeneratorMediator.Send(notification, cancellationToken);
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            return _sourceGeneratorMediator.CreateStream(request, cancellationToken);
        }
    }
}
