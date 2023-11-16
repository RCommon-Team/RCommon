using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using RCommon.Mediator.MediatR;
using System;

namespace RCommon.Mediator.MediatR.Tests
{
    [TestFixture]
    public class MediatrConfigurationTests : MediatRTestBase
    {
        private MockRepository mockRepository;

        private Mock<IServiceCollection> mockServiceCollection;

        public MediatrConfigurationTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }

        [SetUp]
        public void SetUp()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);

            this.mockServiceCollection = this.mockRepository.Create<IServiceCollection>();
        }

        private MediatrConfiguration CreateMediatrConfiguration()
        {
            return new MediatrConfiguration(
                this.mockServiceCollection.Object);
        }

        [Test]
        public void AddMediatr_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var mediatrConfiguration = this.CreateMediatrConfiguration();
            Action options = null;

            // Act
            var result = mediatrConfiguration.AddMediatr(x=>
            {

            });

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }

        [Test]
        public void AddMediatr_StateUnderTest_ExpectedBehavior1()
        {
            // Arrange
            var mediatrConfiguration = this.CreateMediatrConfiguration();
            MediatRServiceConfiguration options = null;

            // Act
            var result = mediatrConfiguration.AddMediatr(
                options);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }
}
