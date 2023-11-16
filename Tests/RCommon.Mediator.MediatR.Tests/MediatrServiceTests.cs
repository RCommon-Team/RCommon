using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using RCommon.Mediator.MediatR;
using System;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Tests
{
    [TestFixture]
    public class MediatrServiceTests : MediatRTestBase
    {
        private MockRepository mockRepository;

        private Mock<IMediator> mockMediator;

        public MediatrServiceTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }

        [SetUp]
        public void SetUp()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);

            this.mockMediator = this.mockRepository.Create<IMediator>();
        }

        private MediatrService CreateService()
        {
            return new MediatrService(
                this.mockMediator.Object);
        }

        [Test]
        public async Task Publish_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var service = this.CreateService();
            object notification = null;
            CancellationToken cancellation = default(global::System.Threading.CancellationToken);

            // Act
            await service.Publish(
                notification,
                cancellation);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }

        [Test]
        public async Task Send_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var service = this.CreateService();
            object notification = null;
            CancellationToken cancellationToken = default(global::System.Threading.CancellationToken);

            // Act
            await service.Send(
                notification,
                cancellationToken);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }

        [Test]
        public void CreateStream_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var service = this.CreateService();
            object request = null;
            CancellationToken cancellationToken = default(global::System.Threading.CancellationToken);

            // Act
            var result = service.CreateStream(
                request,
                cancellationToken);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }
}
