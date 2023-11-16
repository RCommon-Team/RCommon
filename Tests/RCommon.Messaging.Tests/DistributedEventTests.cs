using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Extensions;

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

        [Test]
        public void Can_Construct_With_Empty_Constructor()
        {
            var target = new DistributedEvent();

            Assert.IsNotNull(target);
            Assert.IsNotNull(target.Id);
            Assert.IsNotNull(target.CreationDate);
            Assert.IsTrue(target.Id != Guid.Empty);
            Assert.IsTrue(target.CreationDate.IsValid());
        }

        [Test]
        public void Can_Construct_With_Populated_Constructor()
        {
            var id = Guid.NewGuid();
            var date = DateTime.Now;

            var target = new DistributedEvent(id, date);

            Assert.IsNotNull(target);
            Assert.IsNotNull(target.Id);
            Assert.IsNotNull(target.CreationDate);
            Assert.IsTrue(target.Id == id);
            Assert.IsTrue(target.CreationDate.IsValid());
            Assert.IsTrue(target.CreationDate == date);
        }

        [Test]
        public void Implements_IDistributedEvent()
        {
            var target = new DistributedEvent();
            Assert.That(target is IDistributedEvent);
        }
    }
}
