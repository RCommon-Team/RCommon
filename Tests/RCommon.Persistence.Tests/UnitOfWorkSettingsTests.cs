using FluentAssertions;
using RCommon.Persistence.Transactions;
using System.Transactions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class UnitOfWorkSettingsTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Arrange & Act
        var settings = new UnitOfWorkSettings();

        // Assert
        settings.Should().NotBeNull();
        settings.DefaultIsolation.Should().Be(IsolationLevel.ReadCommitted);
        settings.AutoCompleteScope.Should().BeFalse();
    }

    [Fact]
    public void DefaultIsolation_DefaultValue_IsReadCommitted()
    {
        // Arrange & Act
        var settings = new UnitOfWorkSettings();

        // Assert
        settings.DefaultIsolation.Should().Be(IsolationLevel.ReadCommitted);
    }

    [Fact]
    public void AutoCompleteScope_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var settings = new UnitOfWorkSettings();

        // Assert
        settings.AutoCompleteScope.Should().BeFalse();
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    [InlineData(IsolationLevel.Chaos)]
    [InlineData(IsolationLevel.Unspecified)]
    public void DefaultIsolation_CanBeSet_ToAnyIsolationLevel(IsolationLevel isolationLevel)
    {
        // Arrange
        var settings = new UnitOfWorkSettings();

        // Act
        settings.DefaultIsolation = isolationLevel;

        // Assert
        settings.DefaultIsolation.Should().Be(isolationLevel);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AutoCompleteScope_CanBeSet(bool autoComplete)
    {
        // Arrange
        var settings = new UnitOfWorkSettings();

        // Act
        settings.AutoCompleteScope = autoComplete;

        // Assert
        settings.AutoCompleteScope.Should().Be(autoComplete);
    }

    [Fact]
    public void Settings_CanBeModifiedAfterCreation()
    {
        // Arrange
        var settings = new UnitOfWorkSettings();

        // Act
        settings.DefaultIsolation = IsolationLevel.Serializable;
        settings.AutoCompleteScope = true;

        // Assert
        settings.DefaultIsolation.Should().Be(IsolationLevel.Serializable);
        settings.AutoCompleteScope.Should().BeTrue();
    }

    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange
        var settings1 = new UnitOfWorkSettings();
        var settings2 = new UnitOfWorkSettings();

        // Act
        settings1.DefaultIsolation = IsolationLevel.Serializable;
        settings1.AutoCompleteScope = true;

        // Assert
        settings2.DefaultIsolation.Should().Be(IsolationLevel.ReadCommitted);
        settings2.AutoCompleteScope.Should().BeFalse();
    }

    [Fact]
    public void DefaultIsolation_PropertyGetterAndSetter_WorkCorrectly()
    {
        // Arrange
        var settings = new UnitOfWorkSettings();
        var expectedValue = IsolationLevel.Snapshot;

        // Act
        settings.DefaultIsolation = expectedValue;
        var actualValue = settings.DefaultIsolation;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void AutoCompleteScope_PropertyGetterAndSetter_WorkCorrectly()
    {
        // Arrange
        var settings = new UnitOfWorkSettings();

        // Act
        settings.AutoCompleteScope = true;
        var actualValue = settings.AutoCompleteScope;

        // Assert
        actualValue.Should().BeTrue();
    }
}
