using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RCommon.Models.Events;
using RCommon.Persistence.Sagas;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.Persistence.Tests;

// Test enums
public enum TestSagaStep { Initial, StepOne, StepTwo, Completed }
public enum TestSagaTrigger { GoToOne, GoToTwo, Complete }

// Test saga state
public class TestSagaData : SagaState<Guid>
{
    public string? Payload { get; set; }
}

// Test event
public record TestSagaEvent(Guid CorrelationId) : ISerializableEvent;

// Concrete test saga
public class TestSaga : SagaOrchestrator<TestSagaData, Guid, TestSagaStep, TestSagaTrigger>
{
    public TestSaga(
        ISagaStore<TestSagaData, Guid> store,
        IStateMachineConfigurator<TestSagaStep, TestSagaTrigger> configurator)
        : base(store, configurator) { }

    protected override TestSagaStep InitialState => TestSagaStep.Initial;

    protected override void ConfigureStateMachine(
        IStateMachineConfigurator<TestSagaStep, TestSagaTrigger> configurator)
    {
        configurator.ForState(TestSagaStep.Initial)
            .Permit(TestSagaTrigger.GoToOne, TestSagaStep.StepOne);
        configurator.ForState(TestSagaStep.StepOne)
            .Permit(TestSagaTrigger.GoToTwo, TestSagaStep.StepTwo);
    }

    protected override TestSagaTrigger MapEventToTrigger<TEvent>(TEvent @event)
    {
        return TestSagaTrigger.GoToOne;
    }

    public override Task CompensateAsync(TestSagaData state, CancellationToken ct)
    {
        state.IsFaulted = true;
        state.FaultReason = "Compensated";
        return Task.CompletedTask;
    }
}

public class SagaOrchestratorTests
{
    [Fact]
    public void SagaState_Has_Required_Properties()
    {
        var state = new TestSagaData
        {
            Id = Guid.NewGuid(),
            CorrelationId = "order-123",
            StartedAt = DateTimeOffset.UtcNow,
            CurrentStep = "Initial",
            Version = 1
        };

        state.Id.Should().NotBeEmpty();
        state.CorrelationId.Should().Be("order-123");
        state.IsCompleted.Should().BeFalse();
        state.IsFaulted.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_With_Null_CurrentStep_Uses_InitialState()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(It.IsAny<TestSagaStep>()))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(It.IsAny<TestSagaTrigger>())).Returns(true);
        mockMachine.Setup(m => m.CurrentState).Returns(TestSagaStep.StepOne);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = null! };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state, CancellationToken.None);

        mockConfigurator.Verify(c => c.Build(TestSagaStep.Initial), Times.AtLeastOnce);
        state.CurrentStep.Should().Be("StepOne");
        mockStore.Verify(s => s.SaveAsync(state, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Invalid_Trigger_Is_Ignored()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(It.IsAny<TestSagaStep>()))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(It.IsAny<TestSagaTrigger>())).Returns(false);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state, CancellationToken.None);

        mockMachine.Verify(m => m.FireAsync(It.IsAny<TestSagaTrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        mockStore.Verify(s => s.SaveAsync(It.IsAny<TestSagaData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_With_Known_State_Transitions_Correctly()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(TestSagaStep.Initial))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(TestSagaTrigger.GoToOne)).Returns(true);
        mockMachine.Setup(m => m.CurrentState).Returns(TestSagaStep.StepOne);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state, CancellationToken.None);

        mockConfigurator.Verify(c => c.Build(TestSagaStep.Initial), Times.AtLeastOnce);
        state.CurrentStep.Should().Be("StepOne");
        mockStore.Verify(s => s.SaveAsync(state, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Called_Twice_Configures_StateMachine_Once()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(It.IsAny<TestSagaStep>()))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(It.IsAny<TestSagaTrigger>())).Returns(true);
        mockMachine.Setup(m => m.CurrentState).Returns(TestSagaStep.StepOne);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state1 = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };
        var state2 = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state1, CancellationToken.None);
        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state2, CancellationToken.None);

        // ConfigureStateMachine calls ForState — should only happen once (lazy init)
        // The TestSaga configures 2 states (Initial, StepOne), so ForState is called exactly 2 times total
        mockConfigurator.Verify(c => c.ForState(It.IsAny<TestSagaStep>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CompensateAsync_Sets_Fault_State()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid() };

        await saga.CompensateAsync(state, CancellationToken.None);

        state.IsFaulted.Should().BeTrue();
        state.FaultReason.Should().Be("Compensated");
    }
}
