using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RCommon.Mediator.MediatR.Behaviors;
using System;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Tests.Behaviors
{
    /*[TestFixture]
    public class DistributedUnitOfWorkBehaviorTests : MediatRTestBase
    {
        private MockRepository mockRepository;

        private Mock<IUnitOfWorkFactory> mockUnitOfWorkFactory;
        private Mock<IUnitOfWorkManager> mockUnitOfWorkManager;
        private Mock<ILogger<DistributedUnitOfWorkBehavior<TRequest, TResponse>>> mockLogger;
        private Mock<IDistributedEventPublisher> mockDistributedEventPublisher;

        public DistributedUnitOfWorkBehaviorTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }

        [SetUp]
        public void SetUp()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);

            this.mockUnitOfWorkFactory = this.mockRepository.Create<IUnitOfWorkFactory>();
            this.mockUnitOfWorkManager = this.mockRepository.Create<IUnitOfWorkManager>();
            this.mockLogger = this.mockRepository.Create<ILogger<DistributedUnitOfWorkBehavior<TRequest, TResponse>>>();
            this.mockDistributedEventPublisher = this.mockRepository.Create<IDistributedEventPublisher>();
        }

        private DistributedUnitOfWorkBehavior CreateDistributedUnitOfWorkBehavior()
        {
            return new DistributedUnitOfWorkBehavior(
                this.mockUnitOfWorkFactory.Object,
                this.mockUnitOfWorkManager.Object,
                this.mockLogger.Object,
                this.mockDistributedEventPublisher.Object);
        }

        [Test]
        public async Task Handle_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var distributedUnitOfWorkBehavior = this.CreateDistributedUnitOfWorkBehavior();
            TRequest request = default(TRequest);
            RequestHandlerDelegate next = null;
            CancellationToken cancellationToken = default(global::System.Threading.CancellationToken);

            // Act
            var result = await distributedUnitOfWorkBehavior.Handle(
                request,
                next,
                cancellationToken);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }*/
}
