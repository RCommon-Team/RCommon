using FluentAssertions;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public class OutboxDataStoreRegistryTests
{
    // Helper: build the registry from explicit registration options + a default datastore options value.
    private static OutboxDataStoreRegistry BuildRegistry(
        IEnumerable<string?> pendingNames,
        string? defaultDataStoreName = null)
    {
        var registrationOptions = Options.Create(new OutboxDataStoreRegistrationOptions
        {
            Names = pendingNames.ToList()
        });

        var defaultOptions = Options.Create(new DefaultDataStoreOptions
        {
            DefaultDataStoreName = defaultDataStoreName!
        });

        return new OutboxDataStoreRegistry(registrationOptions, defaultOptions);
    }

    [Fact]
    public void Register_MultipleExplicitNames_AllAppearInRegistrations()
    {
        // Arrange
        var registry = BuildRegistry(new[] { "Orders", "Billing" });

        // Act
        var registrations = registry.Registrations;

        // Assert
        registrations.Should().Contain("Orders");
        registrations.Should().Contain("Billing");
    }

    [Fact]
    public void Register_SameNameTwice_OnlyOneEntryInRegistrations()
    {
        // Arrange
        var registry = BuildRegistry(new[] { "Orders", "Orders" });

        // Act
        var registrations = registry.Registrations;

        // Assert
        registrations.Where(n => n == "Orders").Should().HaveCount(1);
    }

    [Fact]
    public void Register_NamesDifferingOnlyByCase_AreDistinct()
    {
        // Datastore-name comparison is case-SENSITIVE (Ordinal), matching the authoritative core
        // datastore resolver (DataStoreFactory). "Orders" and "orders" are therefore two DISTINCT
        // datastores and both must appear in the registrations.
        var registry = BuildRegistry(new[] { "Orders", "orders" });

        var registrations = registry.Registrations;

        registrations.Should().Contain("Orders");
        registrations.Should().Contain("orders");
        registrations.Should().HaveCount(2);
    }

    [Fact]
    public void Register_NullName_IncludesDefaultDataStoreName_FromOptions()
    {
        // Arrange — null in the pending names list means "use default"
        var registry = BuildRegistry(new string?[] { null }, defaultDataStoreName: "AppDb");

        // Act
        var registrations = registry.Registrations;

        // Assert — the default is resolved lazily from IOptions<DefaultDataStoreOptions>
        registrations.Should().Contain("AppDb");
    }

    [Fact]
    public void Register_NullName_ExplicitNamesAlsoIncluded()
    {
        // Arrange — mix of explicit and default
        var registry = BuildRegistry(new string?[] { "Orders", null }, defaultDataStoreName: "AppDb");

        // Act
        var registrations = registry.Registrations;

        // Assert
        registrations.Should().Contain("Orders");
        registrations.Should().Contain("AppDb");
    }

    [Fact]
    public void NoRegistrations_NoDefault_ReturnsEmpty()
    {
        // Arrange
        var registry = BuildRegistry(Array.Empty<string?>(), defaultDataStoreName: null);

        // Act
        var registrations = registry.Registrations;

        // Assert
        registrations.Should().BeEmpty();
    }

    [Fact]
    public void NoRegistrations_DefaultSet_NoDefaultIncluded_WhenNotMarked()
    {
        // Arrange — no names at all means the default is NOT auto-included
        // (only included when explicitly called with null/empty name via AddOutbox)
        var registry = BuildRegistry(Array.Empty<string?>(), defaultDataStoreName: "AppDb");

        // Act
        var registrations = registry.Registrations;

        // Assert — AppDb should NOT appear because no AddOutbox was called
        registrations.Should().BeEmpty();
    }
}
