using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class EventProducerDedupTests
{
    [Fact]
    public void AddProducer_SameTypeCalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithEventHandling<TestEventHandlingBuilder>(eh =>
        {
            eh.AddProducer<TestProducer>();
            eh.AddProducer<TestProducer>();
        });

        var producerDescriptors = services
            .Where(d => d.ServiceType == typeof(IEventProducer) && d.ImplementationType == typeof(TestProducer))
            .ToList();
        producerDescriptors.Should().HaveCount(1);
    }

    [Fact]
    public void AddProducer_DifferentTypes_RegistersBoth()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithEventHandling<TestEventHandlingBuilder>(eh =>
        {
            eh.AddProducer<TestProducer>();
            eh.AddProducer<OtherTestProducer>();
        });

        services.Count(d => d.ServiceType == typeof(IEventProducer)).Should().Be(2);
    }

    public class TestEventHandlingBuilder : IEventHandlingBuilder
    {
        public TestEventHandlingBuilder(IRCommonBuilder builder) { Services = builder.Services; }
        public IServiceCollection Services { get; }
    }

    public class TestProducer : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }

    public class OtherTestProducer : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }
}
