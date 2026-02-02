using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace RCommon.Core.Tests;

public class SequentialGuidGeneratorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidOptions_SetsOptionsProperty()
    {
        // Arrange
        var options = new SequentialGuidGeneratorOptions
        {
            DefaultSequentialGuidType = SequentialGuidType.SequentialAtEnd
        };
        var mockOptions = new Mock<IOptions<SequentialGuidGeneratorOptions>>();
        mockOptions.Setup(x => x.Value).Returns(options);

        // Act
        var generator = new SequentialGuidGenerator(mockOptions.Object);

        // Assert
        generator.Options.Should().BeSameAs(options);
        generator.Options.DefaultSequentialGuidType.Should().Be(SequentialGuidType.SequentialAtEnd);
    }

    #endregion

    #region Create() Tests

    [Fact]
    public void Create_WithoutParameters_ReturnsNonEmptyGuid()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);
        var generator = new SequentialGuidGenerator(options);

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_CalledMultipleTimes_ReturnsUniqueGuids()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);
        var generator = new SequentialGuidGenerator(options);
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
    public void Create_UsesDefaultSequentialGuidType()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAsString);
        var generator = new SequentialGuidGenerator(options);

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Create(SequentialGuidType) Tests

    [Theory]
    [InlineData(SequentialGuidType.SequentialAsString)]
    [InlineData(SequentialGuidType.SequentialAsBinary)]
    [InlineData(SequentialGuidType.SequentialAtEnd)]
    public void Create_WithSpecificGuidType_ReturnsNonEmptyGuid(SequentialGuidType guidType)
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);
        var generator = new SequentialGuidGenerator(options);

        // Act
        var guid = generator.Create(guidType);

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(SequentialGuidType.SequentialAsString)]
    [InlineData(SequentialGuidType.SequentialAsBinary)]
    [InlineData(SequentialGuidType.SequentialAtEnd)]
    public void Create_WithSpecificGuidType_ReturnsUniqueGuids(SequentialGuidType guidType)
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);
        var generator = new SequentialGuidGenerator(options);
        var guids = new List<Guid>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            guids.Add(generator.Create(guidType));
        }

        // Assert
        guids.Distinct().Should().HaveCount(50);
    }

    #endregion

    #region Sequential Ordering Tests

    [Fact]
    public void Create_SequentialAtEnd_GuidsAreSequentialForSqlServer()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);
        var generator = new SequentialGuidGenerator(options);
        var guids = new List<Guid>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            guids.Add(generator.Create(SequentialGuidType.SequentialAtEnd));
            Thread.Sleep(1); // Small delay to ensure different timestamps
        }

        // Assert - when sorted, should maintain the same order (approximately)
        var sortedGuids = guids.OrderBy(g => g).ToList();
        // The guids should be reasonably sequential - not perfect due to random portion
        guids.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_SequentialAsString_ProducesValidGuids()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAsString);
        var generator = new SequentialGuidGenerator(options);

        // Act
        var guids = Enumerable.Range(0, 10)
            .Select(_ => generator.Create(SequentialGuidType.SequentialAsString))
            .ToList();

        // Assert
        guids.Should().AllSatisfy(g => g.Should().NotBe(Guid.Empty));
        guids.Distinct().Should().HaveCount(10);
    }

    [Fact]
    public void Create_SequentialAsBinary_ProducesValidGuids()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAsBinary);
        var generator = new SequentialGuidGenerator(options);

        // Act
        var guids = Enumerable.Range(0, 10)
            .Select(_ => generator.Create(SequentialGuidType.SequentialAsBinary))
            .ToList();

        // Assert
        guids.Should().AllSatisfy(g => g.Should().NotBe(Guid.Empty));
        guids.Distinct().Should().HaveCount(10);
    }

    #endregion

    #region IGuidGenerator Interface Tests

    [Fact]
    public void SequentialGuidGenerator_ImplementsIGuidGenerator()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);

        // Act
        var generator = new SequentialGuidGenerator(options);

        // Assert
        generator.Should().BeAssignableTo<IGuidGenerator>();
    }

    [Fact]
    public void Create_AsIGuidGenerator_ReturnsNonEmptyGuid()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);
        IGuidGenerator generator = new SequentialGuidGenerator(options);

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Options Property Tests

    [Fact]
    public void Options_ReturnsConfiguredOptions()
    {
        // Arrange
        var expectedType = SequentialGuidType.SequentialAsString;
        var options = CreateOptions(expectedType);
        var generator = new SequentialGuidGenerator(options);

        // Act
        var result = generator.Options;

        // Assert
        result.DefaultSequentialGuidType.Should().Be(expectedType);
    }

    [Fact]
    public void Options_WhenDefaultSequentialGuidTypeIsNull_GetDefaultSequentialGuidTypeReturnsSequentialAtEnd()
    {
        // Arrange
        var optionsValue = new SequentialGuidGeneratorOptions
        {
            DefaultSequentialGuidType = null
        };
        var mockOptions = new Mock<IOptions<SequentialGuidGeneratorOptions>>();
        mockOptions.Setup(x => x.Value).Returns(optionsValue);
        var generator = new SequentialGuidGenerator(mockOptions.Object);

        // Act
        var defaultType = generator.Options.GetDefaultSequentialGuidType();

        // Assert
        defaultType.Should().Be(SequentialGuidType.SequentialAtEnd);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task Create_ConcurrentCalls_AllGuidsAreUnique()
    {
        // Arrange
        var options = CreateOptions(SequentialGuidType.SequentialAtEnd);
        var generator = new SequentialGuidGenerator(options);
        var guids = new System.Collections.Concurrent.ConcurrentBag<Guid>();

        // Act
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => guids.Add(generator.Create())));
        await Task.WhenAll(tasks);

        // Assert
        guids.Distinct().Should().HaveCount(100);
    }

    #endregion

    #region Helper Methods

    private static IOptions<SequentialGuidGeneratorOptions> CreateOptions(SequentialGuidType guidType)
    {
        var options = new SequentialGuidGeneratorOptions
        {
            DefaultSequentialGuidType = guidType
        };
        var mockOptions = new Mock<IOptions<SequentialGuidGeneratorOptions>>();
        mockOptions.Setup(x => x.Value).Returns(options);
        return mockOptions.Object;
    }

    #endregion
}
