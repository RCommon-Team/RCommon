using System;
using System.Threading.Tasks;
using FluentAssertions;
using RCommon.Persistence.Sagas;
using Xunit;

namespace RCommon.Persistence.Tests;

// Use a unique name since TestSagaData already exists in SagaOrchestratorTests.cs
public class InMemoryTestState : SagaState<Guid>
{
    public string? Data { get; set; }
}

public class InMemorySagaStoreTests
{
    [Fact]
    public async Task SaveAsync_And_GetByIdAsync_RoundTrips()
    {
        var store = new InMemorySagaStore<InMemoryTestState, Guid>();
        var state = new InMemoryTestState { Id = Guid.NewGuid(), CorrelationId = "c1", Data = "test" };

        await store.SaveAsync(state);

        var loaded = await store.GetByIdAsync(state.Id);
        loaded.Should().BeSameAs(state);
    }

    [Fact]
    public async Task FindByCorrelationIdAsync_Returns_Matching_State()
    {
        var store = new InMemorySagaStore<InMemoryTestState, Guid>();
        var state = new InMemoryTestState { Id = Guid.NewGuid(), CorrelationId = "order-456" };

        await store.SaveAsync(state);

        var found = await store.FindByCorrelationIdAsync("order-456");
        found.Should().BeSameAs(state);
    }

    [Fact]
    public async Task FindByCorrelationIdAsync_Returns_Null_When_Not_Found()
    {
        var store = new InMemorySagaStore<InMemoryTestState, Guid>();

        var found = await store.FindByCorrelationIdAsync("nonexistent");
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Removes_State()
    {
        var store = new InMemorySagaStore<InMemoryTestState, Guid>();
        var state = new InMemoryTestState { Id = Guid.NewGuid(), CorrelationId = "c1" };

        await store.SaveAsync(state);
        await store.DeleteAsync(state);

        var loaded = await store.GetByIdAsync(state.Id);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_Updates_Existing_State()
    {
        var store = new InMemorySagaStore<InMemoryTestState, Guid>();
        var state = new InMemoryTestState { Id = Guid.NewGuid(), CorrelationId = "c1", Data = "v1" };

        await store.SaveAsync(state);
        state.Data = "v2";
        await store.SaveAsync(state);

        var loaded = await store.GetByIdAsync(state.Id);
        loaded!.Data.Should().Be("v2");
    }
}
