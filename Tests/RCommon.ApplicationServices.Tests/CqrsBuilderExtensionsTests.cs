using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using RCommon;

namespace RCommon.ApplicationServices.Tests
{
    [TestFixture]
    public class CqrsBuilderExtensionsTests
    {
        [Test]
        public void WithCQRS_ShouldReturnBuilder()
        {
            var mockBuilder = new Mock<IRCommonBuilder>();
            var result = CqrsBuilderExtensions.WithCQRS<MockCqrsBuilder>(mockBuilder.Object);

            Assert.That(result, Is.EqualTo(mockBuilder.Object));
        }

        // Helper class for testing
        public class MockCqrsBuilder : ICqrsBuilder
        {
            public IServiceCollection Services { get; } = new ServiceCollection();
        }
    }
}
