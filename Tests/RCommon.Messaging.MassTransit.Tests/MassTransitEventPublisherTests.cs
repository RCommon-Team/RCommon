using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RCommon.EventHandling;
using RCommon.Messaging.MassTransit;
using System;
using System.Threading.Tasks;

namespace RCommon.Messaging.MassTransit.Tests
{
    [TestFixture]
    public class MassTransitEventPublisherTests : MassTransitMessagingTestBase
    {
        private MockRepository _mockRepository;
        private Mock<IPublishEndpoint> _mockPublishEndpoint;
        private ITestHarness _testHarness;

        public MassTransitEventPublisherTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }


        [SetUp]
        public void SetUp()
        {
            this.Logger.LogInformation("Beginning New Test Setup");
            this._mockRepository = new MockRepository(MockBehavior.Strict);

            this._mockPublishEndpoint = this._mockRepository.Create<IPublishEndpoint>();
            this._testHarness = this.ServiceProvider.GetRequiredService<ITestHarness>();
        }

        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup");
        }

        [TearDown]
        public void TearDown()
        {
            this.Logger.LogInformation("Tearing down Test");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            this.Logger.LogInformation("Tearing down Test Suite");
        }

        private MassTransitEventPublisher CreateMassTransitEventPublisher()
        {
            return new MassTransitEventPublisher(
                this._mockPublishEndpoint.Object);
        }

        [Test]
        public void AddDistributedEvent_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var massTransitEventPublisher = this.CreateMassTransitEventPublisher();
            var distributedEvent = new DistributedEvent();

            // Act
            massTransitEventPublisher.AddDistributedEvent(
                distributedEvent);

            // Assert
            Assert.That(massTransitEventPublisher.DistributedEvents.Count == 1);
            this._mockRepository.VerifyAll();
        }

        [Test]
        public void RemoveDistributedEvent_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var massTransitEventPublisher = this.CreateMassTransitEventPublisher();
            var distributedEvent = new DistributedEvent();

            // Act
            massTransitEventPublisher.AddDistributedEvent(
                distributedEvent);

            // Assert
            Assert.That(massTransitEventPublisher.DistributedEvents.Count == 1);

            // Act
            massTransitEventPublisher.RemoveDistributedEvent(
                distributedEvent);

            // Assert
            Assert.That(massTransitEventPublisher.DistributedEvents.Count == 0);

            this._mockRepository.VerifyAll();
        }

        [Test]
        public void ClearDistributedEvents_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var massTransitEventPublisher = this.CreateMassTransitEventPublisher();
            var distributedEvent = new DistributedEvent();

            // Act
            massTransitEventPublisher.AddDistributedEvent(
                distributedEvent);

            // Assert
            Assert.That(massTransitEventPublisher.DistributedEvents.Count == 1);

            // Act
            massTransitEventPublisher.ClearDistributedEvents();

            // Assert
            Assert.That(massTransitEventPublisher.DistributedEvents.Count == 0);
        }

        [Test]
        public async Task PublishDistributedEvents_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var massTransitEventPublisher = this.CreateMassTransitEventPublisher();
            CancellationToken cancellationToken = default(global::System.Threading.CancellationToken);

            var distributedEvent = new DistributedEvent();

            // Act
            massTransitEventPublisher.AddDistributedEvent(
                distributedEvent);

            // Assert
            Assert.That(massTransitEventPublisher.DistributedEvents.Count == 1);

            // Act
            await massTransitEventPublisher.PublishDistributedEvents(
                cancellationToken);

            // TODO: Assert that in-memory MassTransit Test Harness publishing process works

            // Assert
            Assert.That(massTransitEventPublisher.DistributedEvents.Count == 0);
            this._mockRepository.VerifyAll();
        }
    }
}
