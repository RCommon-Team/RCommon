using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using RCommon.ApplicationServices.Queries;
using RCommon.ApplicationServices.Validation;
using RCommon.Caching;
using RCommon.Models.Queries;

namespace RCommon.ApplicationServices.Tests.Queries
{
    [TestFixture]
    public class QueryBusTests
    {
        private Mock<ILogger<QueryBus>> _loggerMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IValidationService> _validationServiceMock;
        private Mock<IOptions<CqrsValidationOptions>> _validationOptionsMock;
        private Mock<IOptions<CachingOptions>> _cachingOptionsMock;
        private CachingOptions _cachingOptions;
        private CqrsValidationOptions _validationOptions;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<QueryBus>>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _validationServiceMock = new Mock<IValidationService>();
            _validationOptions = new CqrsValidationOptions { ValidateQueries = false };
            _validationOptionsMock = new Mock<IOptions<CqrsValidationOptions>>();
            _validationOptionsMock.Setup(x => x.Value).Returns(_validationOptions);
            _cachingOptions = new CachingOptions { CachingEnabled = false, CacheDynamicallyCompiledExpressions = false };
            _cachingOptionsMock = new Mock<IOptions<CachingOptions>>();
            _cachingOptionsMock.Setup(x => x.Value).Returns(_cachingOptions);
        }

        public class TestQuery : IQuery<string> { }

        public class TestQueryHandler : IQueryHandler<TestQuery, string>
        {
            public Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken)
            {
                return Task.FromResult("handled");
            }
        }

        [Test]
        public async Task DispatchQueryAsync_ValidatesQuery_WhenValidationEnabled()
        {
            _validationOptions.ValidateQueries = true;
            var query = new TestQuery();
            _validationServiceMock
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationOutcome())
                .Verifiable();

            var handler = new TestQueryHandler();
            _serviceProviderMock
                .Setup(x => x.GetRequiredService(typeof(IQueryHandler<TestQuery, string>)))
                .Returns(handler);

            var queryBus = new QueryBus(
                _loggerMock.Object,
                _serviceProviderMock.Object,
                _validationServiceMock.Object,
                _validationOptionsMock.Object,
                _cachingOptionsMock.Object
            );

            var result = await queryBus.DispatchQueryAsync(query);

            Assert.That(result, Is.EqualTo("handled"));
            _validationServiceMock.Verify();
        }

        [Test]
        public async Task DispatchQueryAsync_ResolvesHandlerAndReturnsResult()
        {
            var query = new TestQuery();
            var handler = new TestQueryHandler();
            _serviceProviderMock
                .Setup(x => x.GetRequiredService(typeof(IQueryHandler<TestQuery, string>)))
                .Returns(handler);

            var queryBus = new QueryBus(
                _loggerMock.Object,
                _serviceProviderMock.Object,
                _validationServiceMock.Object,
                _validationOptionsMock.Object,
                _cachingOptionsMock.Object
            );

            var result = await queryBus.DispatchQueryAsync(query);

            Assert.That(result, Is.EqualTo("handled"));
        }
    }
}
