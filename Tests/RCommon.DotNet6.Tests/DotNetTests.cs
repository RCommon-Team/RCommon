using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace RCommon.DotNet6.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Can_Use_Dapper()
        {
            var services = new ServiceCollection();
            var target = new DapperPersistenceBuilder(services);
            Assert.That(target, Is.Not.Null);
        }
    }
}
