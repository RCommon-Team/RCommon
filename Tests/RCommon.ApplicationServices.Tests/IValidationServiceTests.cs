using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using RCommon.ApplicationServices.Validation;

namespace RCommon.ApplicationServices.Tests.Validation
{
    [TestFixture]
    public class IValidationServiceTests
    {
        private Mock<IValidationService> _mockValidationService;

        [SetUp]
        public void SetUp()
        {
            _mockValidationService = new Mock<IValidationService>();
        }

        [Test]
        public async Task ValidateAsync_ShouldReturnValidationOutcome()
        {
            var target = new object();
            var expectedOutcome = new ValidationOutcome();
            _mockValidationService
                .Setup(x => x.ValidateAsync<object>(It.IsAny<object>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedOutcome);

            var result = await _mockValidationService.Object.ValidateAsync(target);

            Assert.That(result, Is.EqualTo(expectedOutcome));
        }
    }
}
