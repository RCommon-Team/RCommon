using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class SimpleGuidGeneratorTests
{
    #region Create Tests

    [Fact]
    public void Create_ReturnsNonEmptyGuid()
    {
        // Arrange
        var generator = new SimpleGuidGenerator();

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_CalledMultipleTimes_ReturnsUniqueGuids()
    {
        // Arrange
        var generator = new SimpleGuidGenerator();
        var guids = new List<Guid>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            guids.Add(generator.Create());
        }

        // Assert
        guids.Distinct().Should().HaveCount(100);
    }

    [Fact]
    public void Create_ReturnsValidGuidFormat()
    {
        // Arrange
        var generator = new SimpleGuidGenerator();

        // Act
        var guid = generator.Create();
        var guidString = guid.ToString();

        // Assert
        Guid.TryParse(guidString, out _).Should().BeTrue();
    }

    #endregion

    #region IGuidGenerator Interface Tests

    [Fact]
    public void SimpleGuidGenerator_ImplementsIGuidGenerator()
    {
        // Arrange & Act
        var generator = new SimpleGuidGenerator();

        // Assert
        generator.Should().BeAssignableTo<IGuidGenerator>();
    }

    [Fact]
    public void Create_AsIGuidGenerator_ReturnsNonEmptyGuid()
    {
        // Arrange
        IGuidGenerator generator = new SimpleGuidGenerator();

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task Create_ConcurrentCalls_AllGuidsAreUnique()
    {
        // Arrange
        var generator = new SimpleGuidGenerator();
        var guids = new System.Collections.Concurrent.ConcurrentBag<Guid>();

        // Act
        var tasks = Enumerable.Range(0, 1000)
            .Select(_ => Task.Run(() => guids.Add(generator.Create())));
        await Task.WhenAll(tasks);

        // Assert
        guids.Distinct().Should().HaveCount(1000);
    }

    #endregion

    #region Virtual Method Tests

    [Fact]
    public void Create_IsVirtual_CanBeOverridden()
    {
        // Arrange
        var customGenerator = new CustomGuidGenerator();

        // Act
        var guid = customGenerator.Create();

        // Assert
        guid.Should().Be(CustomGuidGenerator.FixedGuid);
    }

    #endregion

    #region Test Helper Classes

    private class CustomGuidGenerator : SimpleGuidGenerator
    {
        public static readonly Guid FixedGuid = new Guid("12345678-1234-1234-1234-123456789012");

        public override Guid Create()
        {
            return FixedGuid;
        }
    }

    #endregion
}
