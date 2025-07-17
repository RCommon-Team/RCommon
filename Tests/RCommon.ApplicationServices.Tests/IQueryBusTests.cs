using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using RCommon.ApplicationServices.Queries;
using RCommon.Models.Queries;

namespace RCommon.ApplicationServices.Tests.Queries
{
    [TestFixture]
    public class IQueryBusTests
    {
        private Mock<IQueryBus> _mockQueryBus;

        [SetUp]
        public void SetUp()
        {
            _mockQueryBus = new Mock<IQueryBus>();
        }

        [Test]
        public async Task DispatchQueryAsync_ShouldReturnResult()
        {
            var query = new Mock<IQuery<string>>().Object;
            var expectedResult = "result";
            _mockQueryBus
                .Setup(x => x.DispatchQueryAsync(It.IsAny<IQuery<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await _mockQueryBus.Object.DispatchQueryAsync(query);

            Assert.That(result, Is.EqualTo(expectedResult));
        }
    }
}
