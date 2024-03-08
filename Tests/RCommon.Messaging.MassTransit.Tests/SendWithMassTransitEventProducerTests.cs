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
    public class SendWithMassTransitEventProducerTests : MassTransitMessagingTestBase
    {
        private MockRepository _mockRepository;
        private Mock<IPublishEndpoint> _mockPublishEndpoint;
        private ITestHarness _testHarness;

        public SendWithMassTransitEventProducerTests() : base()
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

      
    }
}
