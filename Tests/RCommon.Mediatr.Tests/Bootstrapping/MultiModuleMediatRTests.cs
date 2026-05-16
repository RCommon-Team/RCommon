using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using RCommon.MediatR;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Mediatr.Tests.Bootstrapping;

public class MultiModuleMediatRTests
{
    [Fact]
    public void TwoModules_DistinctProducers_BothRegister()
    {
        var services = new ServiceCollection();

        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerA>());
        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerB>());

        services.Count(d => d.ServiceType == typeof(IEventProducer)).Should().Be(2);
    }

    [Fact]
    public void TwoModules_SameProducer_RegistersOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerA>());
        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerA>());

        services.Count(d =>
            d.ServiceType == typeof(IEventProducer) && d.ImplementationType == typeof(TestProducerA))
            .Should().Be(1);
    }

    public class TestProducerA : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }

    public class TestProducerB : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }
}
