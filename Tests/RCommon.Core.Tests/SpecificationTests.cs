using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class SpecificationTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidPredicate_CreatesSpecification()
    {
        // Arrange & Act
        var spec = new Specification<TestEntity>(x => x.Id > 0);

        // Assert
        spec.Should().NotBeNull();
        spec.Predicate.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new Specification<TestEntity>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Predicate Tests

    [Fact]
    public void Predicate_ReturnsExpressionUsedInConstruction()
    {
        // Arrange
        var spec = new Specification<TestEntity>(x => x.Name == "Test");

        // Act
        var predicate = spec.Predicate;

        // Assert
        predicate.Should().NotBeNull();
        predicate.Compile()(new TestEntity { Name = "Test" }).Should().BeTrue();
        predicate.Compile()(new TestEntity { Name = "Other" }).Should().BeFalse();
    }

    #endregion

    #region IsSatisfiedBy Tests

    [Fact]
    public void IsSatisfiedBy_WhenEntityMatchesPredicate_ReturnsTrue()
    {
        // Arrange
        var spec = new Specification<TestEntity>(x => x.Id > 10);
        var entity = new TestEntity { Id = 15 };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WhenEntityDoesNotMatchPredicate_ReturnsFalse()
    {
        // Arrange
        var spec = new Specification<TestEntity>(x => x.Id > 10);
        var entity = new TestEntity { Id = 5 };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithStringPredicate_EvaluatesCorrectly()
    {
        // Arrange
        var spec = new Specification<TestEntity>(x => x.Name != null && x.Name.StartsWith("Test"));
        var matchingEntity = new TestEntity { Name = "Test Entity" };
        var nonMatchingEntity = new TestEntity { Name = "Other Entity" };

        // Act & Assert
        spec.IsSatisfiedBy(matchingEntity).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatchingEntity).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithComplexPredicate_EvaluatesCorrectly()
    {
        // Arrange
        var spec = new Specification<TestEntity>(x => x.Id > 0 && x.IsActive && x.Name != null);
        var matchingEntity = new TestEntity { Id = 1, IsActive = true, Name = "Test" };
        var nonMatchingEntity = new TestEntity { Id = 1, IsActive = false, Name = "Test" };

        // Act & Assert
        spec.IsSatisfiedBy(matchingEntity).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatchingEntity).Should().BeFalse();
    }

    #endregion

    #region And Operator Tests

    [Fact]
    public void AndOperator_CombinesTwoSpecifications_BothMustBeSatisfied()
    {
        // Arrange
        var spec1 = new Specification<TestEntity>(x => x.Id > 0);
        var spec2 = new Specification<TestEntity>(x => x.IsActive);
        var combinedSpec = spec1 & spec2;

        var satisfiesBoth = new TestEntity { Id = 1, IsActive = true };
        var satisfiesFirst = new TestEntity { Id = 1, IsActive = false };
        var satisfiesSecond = new TestEntity { Id = 0, IsActive = true };
        var satisfiesNeither = new TestEntity { Id = 0, IsActive = false };

        // Act & Assert
        combinedSpec.IsSatisfiedBy(satisfiesBoth).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesFirst).Should().BeFalse();
        combinedSpec.IsSatisfiedBy(satisfiesSecond).Should().BeFalse();
        combinedSpec.IsSatisfiedBy(satisfiesNeither).Should().BeFalse();
    }

    [Fact]
    public void AndOperator_ReturnsNewSpecification()
    {
        // Arrange
        var spec1 = new Specification<TestEntity>(x => x.Id > 0);
        var spec2 = new Specification<TestEntity>(x => x.IsActive);

        // Act
        var combinedSpec = spec1 & spec2;

        // Assert
        combinedSpec.Should().NotBeNull();
        combinedSpec.Should().NotBeSameAs(spec1);
        combinedSpec.Should().NotBeSameAs(spec2);
    }

    [Fact]
    public void AndOperator_ChainedMultipleSpecifications_AllMustBeSatisfied()
    {
        // Arrange
        var spec1 = new Specification<TestEntity>(x => x.Id > 0);
        var spec2 = new Specification<TestEntity>(x => x.IsActive);
        var spec3 = new Specification<TestEntity>(x => x.Name != null);
        var combinedSpec = spec1 & spec2 & spec3;

        var satisfiesAll = new TestEntity { Id = 1, IsActive = true, Name = "Test" };
        var missingName = new TestEntity { Id = 1, IsActive = true, Name = null };

        // Act & Assert
        combinedSpec.IsSatisfiedBy(satisfiesAll).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(missingName).Should().BeFalse();
    }

    #endregion

    #region Or Operator Tests

    [Fact]
    public void OrOperator_CombinesTwoSpecifications_EitherCanBeSatisfied()
    {
        // Arrange
        var spec1 = new Specification<TestEntity>(x => x.Id > 10);
        var spec2 = new Specification<TestEntity>(x => x.IsActive);
        var combinedSpec = spec1 | spec2;

        var satisfiesBoth = new TestEntity { Id = 15, IsActive = true };
        var satisfiesFirst = new TestEntity { Id = 15, IsActive = false };
        var satisfiesSecond = new TestEntity { Id = 5, IsActive = true };
        var satisfiesNeither = new TestEntity { Id = 5, IsActive = false };

        // Act & Assert
        combinedSpec.IsSatisfiedBy(satisfiesBoth).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesFirst).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesSecond).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesNeither).Should().BeFalse();
    }

    [Fact]
    public void OrOperator_ReturnsNewSpecification()
    {
        // Arrange
        var spec1 = new Specification<TestEntity>(x => x.Id > 0);
        var spec2 = new Specification<TestEntity>(x => x.IsActive);

        // Act
        var combinedSpec = spec1 | spec2;

        // Assert
        combinedSpec.Should().NotBeNull();
        combinedSpec.Should().NotBeSameAs(spec1);
        combinedSpec.Should().NotBeSameAs(spec2);
    }

    [Fact]
    public void OrOperator_ChainedMultipleSpecifications_AnyCanBeSatisfied()
    {
        // Arrange
        var spec1 = new Specification<TestEntity>(x => x.Id > 100);
        var spec2 = new Specification<TestEntity>(x => x.Name == "Special");
        var spec3 = new Specification<TestEntity>(x => x.IsActive && x.Id > 50);
        var combinedSpec = spec1 | spec2 | spec3;

        var satisfiesFirst = new TestEntity { Id = 150, IsActive = false, Name = "Normal" };
        var satisfiesSecond = new TestEntity { Id = 1, IsActive = false, Name = "Special" };
        var satisfiesThird = new TestEntity { Id = 75, IsActive = true, Name = "Normal" };
        var satisfiesNone = new TestEntity { Id = 25, IsActive = false, Name = "Normal" };

        // Act & Assert
        combinedSpec.IsSatisfiedBy(satisfiesFirst).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesSecond).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesThird).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesNone).Should().BeFalse();
    }

    #endregion

    #region Combined And/Or Operator Tests

    [Fact]
    public void CombinedAndOrOperators_EvaluatesCorrectly()
    {
        // Arrange
        var activeSpec = new Specification<TestEntity>(x => x.IsActive);
        var highIdSpec = new Specification<TestEntity>(x => x.Id > 100);
        var specialNameSpec = new Specification<TestEntity>(x => x.Name == "Special");

        // (Active AND HighId) OR SpecialName
        var combinedSpec = (activeSpec & highIdSpec) | specialNameSpec;

        var satisfiesActiveAndHighId = new TestEntity { Id = 150, IsActive = true, Name = "Normal" };
        var satisfiesSpecialName = new TestEntity { Id = 5, IsActive = false, Name = "Special" };
        var satisfiesNone = new TestEntity { Id = 50, IsActive = true, Name = "Normal" };

        // Act & Assert
        combinedSpec.IsSatisfiedBy(satisfiesActiveAndHighId).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesSpecialName).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(satisfiesNone).Should().BeFalse();
    }

    #endregion

    #region Test Helper Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}
