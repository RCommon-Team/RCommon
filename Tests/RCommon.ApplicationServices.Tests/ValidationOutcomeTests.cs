using FluentAssertions;
using RCommon.ApplicationServices.Validation;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class ValidationOutcomeTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyErrors()
    {
        // Arrange & Act
        var outcome = new ValidationOutcome();

        // Assert
        outcome.Errors.Should().NotBeNull();
        outcome.Errors.Should().BeEmpty();
        outcome.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithFailures_PopulatesErrors()
    {
        // Arrange
        var failures = new List<ValidationFault>
        {
            new ValidationFault("Property1", "Error 1"),
            new ValidationFault("Property2", "Error 2")
        };

        // Act
        var outcome = new ValidationOutcome(failures);

        // Assert
        outcome.Errors.Should().HaveCount(2);
        outcome.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithFailures_FiltersNulls()
    {
        // Arrange
        var failures = new List<ValidationFault?>
        {
            new ValidationFault("Property1", "Error 1"),
            null,
            new ValidationFault("Property2", "Error 2")
        };

        // Act
        var outcome = new ValidationOutcome(failures!);

        // Assert
        outcome.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_WithOtherResults_CombinesErrors()
    {
        // Arrange
        var result1 = new ValidationOutcome(new[] { new ValidationFault("Prop1", "Error1") });
        var result2 = new ValidationOutcome(new[] { new ValidationFault("Prop2", "Error2") });
        var results = new[] { result1, result2 };

        // Act
        var combined = new ValidationOutcome(results);

        // Assert
        combined.Errors.Should().HaveCount(2);
        combined.Errors.Should().Contain(e => e.PropertyName == "Prop1");
        combined.Errors.Should().Contain(e => e.PropertyName == "Prop2");
    }

    [Fact]
    public void Constructor_WithOtherResults_CombinesRuleSetsExecuted()
    {
        // Arrange
        var result1 = new ValidationOutcome { RuleSetsExecuted = new[] { "RuleSet1", "RuleSet2" } };
        var result2 = new ValidationOutcome { RuleSetsExecuted = new[] { "RuleSet2", "RuleSet3" } };
        var results = new[] { result1, result2 };

        // Act
        var combined = new ValidationOutcome(results);

        // Assert
        combined.RuleSetsExecuted.Should().HaveCount(3);
        combined.RuleSetsExecuted.Should().Contain("RuleSet1");
        combined.RuleSetsExecuted.Should().Contain("RuleSet2");
        combined.RuleSetsExecuted.Should().Contain("RuleSet3");
    }

    [Fact]
    public void IsValid_WithNoErrors_ReturnsTrue()
    {
        // Arrange
        var outcome = new ValidationOutcome();

        // Act & Assert
        outcome.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithErrors_ReturnsFalse()
    {
        // Arrange
        var outcome = new ValidationOutcome(new[] { new ValidationFault("Prop", "Error") });

        // Act & Assert
        outcome.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Errors_SetterWithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var outcome = new ValidationOutcome();

        // Act
        var act = () => outcome.Errors = null!;

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Errors_SetterWithList_FiltersNullsAndCopies()
    {
        // Arrange
        var outcome = new ValidationOutcome();
        var newErrors = new List<ValidationFault?>
        {
            new ValidationFault("Prop1", "Error1"),
            null,
            new ValidationFault("Prop2", "Error2")
        };

        // Act
        outcome.Errors = newErrors!;

        // Assert
        outcome.Errors.Should().HaveCount(2);
        outcome.Errors.Should().NotContainNulls();
    }

    [Fact]
    public void ToString_Default_ReturnsErrorsSeparatedByNewLine()
    {
        // Arrange
        var outcome = new ValidationOutcome(new[]
        {
            new ValidationFault("Prop1", "Error 1"),
            new ValidationFault("Prop2", "Error 2")
        });

        // Act
        var result = outcome.ToString();

        // Assert
        result.Should().Contain("Error 1");
        result.Should().Contain("Error 2");
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void ToString_WithCustomSeparator_ReturnsErrorsSeparatedByCustomSeparator()
    {
        // Arrange
        var outcome = new ValidationOutcome(new[]
        {
            new ValidationFault("Prop1", "Error 1"),
            new ValidationFault("Prop2", "Error 2")
        });

        // Act
        var result = outcome.ToString(" | ");

        // Assert
        result.Should().Be("Error 1 | Error 2");
    }

    [Fact]
    public void ToString_WithNoErrors_ReturnsEmptyString()
    {
        // Arrange
        var outcome = new ValidationOutcome();

        // Act
        var result = outcome.ToString();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToDictionary_GroupsErrorsByPropertyName()
    {
        // Arrange
        var outcome = new ValidationOutcome(new[]
        {
            new ValidationFault("Prop1", "Error 1a"),
            new ValidationFault("Prop1", "Error 1b"),
            new ValidationFault("Prop2", "Error 2")
        });

        // Act
        var dictionary = outcome.ToDictionary();

        // Assert
        dictionary.Should().HaveCount(2);
        dictionary["Prop1"].Should().HaveCount(2);
        dictionary["Prop1"].Should().Contain("Error 1a");
        dictionary["Prop1"].Should().Contain("Error 1b");
        dictionary["Prop2"].Should().HaveCount(1);
        dictionary["Prop2"].Should().Contain("Error 2");
    }

    [Fact]
    public void ToDictionary_WithNoErrors_ReturnsEmptyDictionary()
    {
        // Arrange
        var outcome = new ValidationOutcome();

        // Act
        var dictionary = outcome.ToDictionary();

        // Assert
        dictionary.Should().BeEmpty();
    }

    [Fact]
    public void RuleSetsExecuted_CanBeSetAndGet()
    {
        // Arrange
        var outcome = new ValidationOutcome();
        var ruleSets = new[] { "RuleSet1", "RuleSet2" };

        // Act
        outcome.RuleSetsExecuted = ruleSets;

        // Assert
        outcome.RuleSetsExecuted.Should().BeEquivalentTo(ruleSets);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(5, false)]
    public void IsValid_DependsOnErrorCount(int errorCount, bool expectedIsValid)
    {
        // Arrange
        var errors = Enumerable.Range(0, errorCount)
            .Select(i => new ValidationFault($"Prop{i}", $"Error {i}"))
            .ToList();
        var outcome = new ValidationOutcome(errors);

        // Act & Assert
        outcome.IsValid.Should().Be(expectedIsValid);
    }
}
