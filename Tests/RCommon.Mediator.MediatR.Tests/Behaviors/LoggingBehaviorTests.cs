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
    public class LoggingBehaviorTests : MediatRTestBase
    {
        private MockRepository mockRepository;

        private Mock<ILogger<LoggingBehavior<TRequest, TResponse>>> mockLogger;

        public LoggingBehaviorTests()
         : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }

        [SetUp]
        public void SetUp()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);

            this.mockLogger = this.mockRepository.Create<ILogger<LoggingBehavior<TRequest, TResponse>>>();
        }

        private LoggingBehavior CreateLoggingBehavior()
        {
            return new LoggingBehavior(
                this.mockLogger.Object);
        }

        [Test]
        public async Task Handle_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var loggingBehavior = this.CreateLoggingBehavior();
            TRequest request = default(TRequest);
            RequestHandlerDelegate next = null;
            CancellationToken cancellationToken = default(global::System.Threading.CancellationToken);

            // Act
            var result = await loggingBehavior.Handle(
                request,
                next,
                cancellationToken);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }*/
}
