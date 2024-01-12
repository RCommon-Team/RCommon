using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RCommon.Messaging.Tests
{
    [TestFixture()]
    public class DistributedEventTests : MessagingTestBase
    {
        public DistributedEventTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }


        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup");
        }

        [SetUp]
        public void Setup()
        {
            this.Logger.LogInformation("Beginning New Test Setup");
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
