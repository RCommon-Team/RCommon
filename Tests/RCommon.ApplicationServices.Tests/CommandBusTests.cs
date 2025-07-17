using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.Validation;
using RCommon.Caching;
using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;

namespace RCommon.ApplicationServices.Tests.Commands
{
    [TestFixture]
    public class CommandBusTests
    {
        private Mock<ILogger<CommandBus>> _loggerMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IValidationService> _validationServiceMock;
        private Mock<IOptions<CqrsValidationOptions>> _validationOptionsMock;
        private Mock<IOptions<CachingOptions>> _cachingOptionsMock;
        private CachingOptions _cachingOptions;
        private CqrsValidationOptions _validationOptions;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<CommandBus>>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _validationServiceMock = new Mock<IValidationService>();
            _validationOptions = new CqrsValidationOptions { ValidateCommands = false };
            _validationOptionsMock = new Mock<IOptions<CqrsValidationOptions>>();
            _validationOptionsMock.Setup(x => x.Value).Returns(_validationOptions);
            _cachingOptions = new CachingOptions { CachingEnabled = false, CacheDynamicallyCompiledExpressions = false };
            _cachingOptionsMock = new Mock<IOptions<CachingOptions>>();
            _cachingOptionsMock.Setup(x => x.Value).Returns(_cachingOptions);
        }

        public class TestResult : IExecutionResult
        {
            public bool IsSuccess { get; set; }
        }

        public class TestCommand : ICommand<TestResult> { }

        public class TestCommandHandler : ICommandHandler<TestResult, TestCommand>
        {
            public Task<TestResult> HandleAsync(TestCommand command, CancellationToken cancellationToken)
            {
                return Task.FromResult(new TestResult { IsSuccess = true });
            }
        }

        [Test]
        public async Task DispatchCommandAsync_ValidatesCommand_WhenValidationEnabled()
        {
            _validationOptions.ValidateCommands = true;
            var command = new TestCommand();
            _validationServiceMock
                .Setup(x => x.ValidateAsync(command, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationOutcome())
                .Verifiable();

            var handler = new TestCommandHandler();
            _serviceProviderMock
                .Setup(x => x.GetServices(typeof(ICommandHandler<TestResult, TestCommand>)))
                .Returns(new List<ICommandHandler> { handler });

            var commandBus = new CommandBus(
                _loggerMock.Object,
                _serviceProviderMock.Object,
                _validationServiceMock.Object,
                _validationOptionsMock.Object,
                _cachingOptionsMock.Object
            );

            var result = await commandBus.DispatchCommandAsync(command);

            Assert.That(result.IsSuccess, Is.True);
            _validationServiceMock.Verify();
        }

        [Test]
        public async Task DispatchCommandAsync_ResolvesHandlerAndReturnsResult()
        {
            var command = new TestCommand();
            var handler = new TestCommandHandler();
            _serviceProviderMock
                .Setup(x => x.GetServices(typeof(ICommandHandler<TestResult, TestCommand>)))
                .Returns(new List<ICommandHandler> { handler });

            var commandBus = new CommandBus(
                _loggerMock.Object,
                _serviceProviderMock.Object,
                _validationServiceMock.Object,
                _validationOptionsMock.Object,
                _cachingOptionsMock.Object
            );

            var result = await commandBus.DispatchCommandAsync(command);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void DispatchCommandAsync_ThrowsIfNoHandlerRegistered()
        {
            var command = new TestCommand();
            _serviceProviderMock
                .Setup(x => x.GetServices(typeof(ICommandHandler<TestResult, TestCommand>)))
                .Returns(new List<ICommandHandler>());

            var commandBus = new CommandBus(
                _loggerMock.Object,
                _serviceProviderMock.Object,
                _validationServiceMock.Object,
                _validationOptionsMock.Object,
                _cachingOptionsMock.Object
            );

            Assert.ThrowsAsync<NoCommandHandlersException>(async () =>
                await commandBus.DispatchCommandAsync(command));
        }

        [Test]
        public void DispatchCommandAsync_ThrowsIfMultipleHandlersRegistered()
        {
            var command = new TestCommand();
            var handler1 = new TestCommandHandler();
            var handler2 = new TestCommandHandler();
            _serviceProviderMock
                .Setup(x => x.GetServices(typeof(ICommandHandler<TestResult, TestCommand>)))
                .Returns(new List<ICommandHandler> { handler1, handler2 });

            var commandBus = new CommandBus(
                _loggerMock.Object,
                _serviceProviderMock.Object,
                _validationServiceMock.Object,
                _validationOptionsMock.Object,
                _cachingOptionsMock.Object
            );

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await commandBus.DispatchCommandAsync(command));
        }
    }
}
