using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RCommon.Mediator.MediatR.Behaviors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Tests.Behaviors
{
    [TestFixture]
    public class ValidatorBehaviorTests : MediatRTestBase
    {
        private MockRepository mockRepository;

        private Mock<IEnumerable<IValidator<TRequest>>> mockEnumerable;
        private Mock<ILogger<ValidatorBehavior<TRequest, TResponse>>> mockLogger;

        public ValidatorBehaviorTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }

        [SetUp]
        public void SetUp()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);

            this.mockEnumerable = this.mockRepository.Create<IEnumerable<IValidator<TRequest>>>();
            this.mockLogger = this.mockRepository.Create<ILogger<ValidatorBehavior<TRequest, TResponse>>>();
        }

        private ValidatorBehavior CreateValidatorBehavior()
        {
            return new ValidatorBehavior(
                this.mockEnumerable.Object,
                this.mockLogger.Object);
        }

        [Test]
        public async Task Handle_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var validatorBehavior = this.CreateValidatorBehavior();
            TRequest request = default(TRequest);
            RequestHandlerDelegate next = null;
            CancellationToken cancellationToken = default(global::System.Threading.CancellationToken);

            // Act
            var result = await validatorBehavior.Handle(
                request,
                next,
                cancellationToken);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }
}
