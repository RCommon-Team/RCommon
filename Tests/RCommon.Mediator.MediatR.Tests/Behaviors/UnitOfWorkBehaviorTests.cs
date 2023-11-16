using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RCommon.DataServices.Transactions;
using RCommon.Mediator.MediatR.Behaviors;
using System;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Tests.Behaviors
{
   /* [TestFixture]
    public class UnitOfWorkBehaviorTests : MediatRTestBase
    {
        private MockRepository mockRepository;

        private Mock<IUnitOfWorkFactory> mockUnitOfWorkFactory;
        private Mock<IUnitOfWorkManager> mockUnitOfWorkManager;
        private Mock<ILogger<UnitOfWorkBehavior<TRequest, TResponse>>> mockLogger;

        public UnitOfWorkBehaviorTests() : base()
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
            this.mockLogger = this.mockRepository.Create<ILogger<UnitOfWorkBehavior<TRequest, TResponse>>>();
        }

        private UnitOfWorkBehavior CreateUnitOfWorkBehavior()
        {
            return new UnitOfWorkBehavior(
                this.mockUnitOfWorkFactory.Object,
                this.mockUnitOfWorkManager.Object,
                this.mockLogger.Object);
        }

        [Test]
        public async Task Handle_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var unitOfWorkBehavior = this.CreateUnitOfWorkBehavior();
            TRequest request = default(TRequest);
            RequestHandlerDelegate next = null;
            CancellationToken cancellationToken = default(global::System.Threading.CancellationToken);

            // Act
            var result = await unitOfWorkBehavior.Handle(
                request,
                next,
                cancellationToken);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }*/
}
