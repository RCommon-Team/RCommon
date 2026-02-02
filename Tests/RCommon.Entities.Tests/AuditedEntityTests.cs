using Bogus;
using FluentAssertions;
using RCommon.Entities;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for AuditedEntity classes.
/// </summary>
public class AuditedEntityTests
{
    private readonly Faker _faker;

    public AuditedEntityTests()
    {
        _faker = new Faker();
    }

    #region Test Entities

    /// <summary>
    /// Concrete implementation of AuditedEntity with string user types (no key).
    /// </summary>
    private class TestAuditedEntityNoKey : AuditedEntity<string, string>
    {
        public string Name { get; set; } = string.Empty;

        public override object[] GetKeys()
        {
            return new object[] { Name };
        }
    }

    /// <summary>
    /// Concrete implementation of AuditedEntity with int key and string user types.
    /// </summary>
    private class TestAuditedEntityIntKey : AuditedEntity<int, string, string>
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Concrete implementation of AuditedEntity with Guid key and string user types.
    /// </summary>
    private class TestAuditedEntityGuidKey : AuditedEntity<Guid, string, string>
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Concrete implementation of AuditedEntity with int key and int user types.
    /// </summary>
    private class TestAuditedEntityWithIntUsers : AuditedEntity<int, int, int>
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Concrete implementation of AuditedEntity with mixed user types.
    /// </summary>
    private class TestAuditedEntityMixedUsers : AuditedEntity<int, string, Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// User class for testing complex user types.
    /// </summary>
    private class TestUser
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// Concrete implementation with complex user types.
    /// </summary>
    private class TestAuditedEntityWithComplexUsers : AuditedEntity<int, TestUser, TestUser>
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test event implementing ISerializableEvent.
    /// </summary>
    private class TestSerializableEvent : ISerializableEvent
    {
        public string EventName { get; set; } = string.Empty;
    }

    #endregion

    #region AuditedEntity (No Key) Tests

    [Fact]
    public void AuditedEntityNoKey_DefaultConstructor_InitializesWithNullValues()
    {
        // Arrange & Act
        var entity = new TestAuditedEntityNoKey();

        // Assert
        entity.DateCreated.Should().BeNull();
        entity.CreatedBy.Should().BeNull();
        entity.DateLastModified.Should().BeNull();
        entity.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void AuditedEntityNoKey_DateCreated_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityNoKey();
        var expectedDate = _faker.Date.Past();

        // Act
        entity.DateCreated = expectedDate;

        // Assert
        entity.DateCreated.Should().Be(expectedDate);
    }

    [Fact]
    public void AuditedEntityNoKey_CreatedBy_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityNoKey();
        var expectedUser = _faker.Internet.UserName();

        // Act
        entity.CreatedBy = expectedUser;

        // Assert
        entity.CreatedBy.Should().Be(expectedUser);
    }

    [Fact]
    public void AuditedEntityNoKey_DateLastModified_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityNoKey();
        var expectedDate = _faker.Date.Recent();

        // Act
        entity.DateLastModified = expectedDate;

        // Assert
        entity.DateLastModified.Should().Be(expectedDate);
    }

    [Fact]
    public void AuditedEntityNoKey_LastModifiedBy_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityNoKey();
        var expectedUser = _faker.Internet.UserName();

        // Act
        entity.LastModifiedBy = expectedUser;

        // Assert
        entity.LastModifiedBy.Should().Be(expectedUser);
    }

    [Fact]
    public void AuditedEntityNoKey_ImplementsIBusinessEntity()
    {
        // Arrange & Act
        var entity = new TestAuditedEntityNoKey();

        // Assert
        entity.Should().BeAssignableTo<IBusinessEntity>();
    }

    [Fact]
    public void AuditedEntityNoKey_ImplementsIAuditedEntity()
    {
        // Arrange & Act
        var entity = new TestAuditedEntityNoKey();

        // Assert
        entity.Should().BeAssignableTo<IAuditedEntity<string, string>>();
    }

    #endregion

    #region AuditedEntity<TKey> Tests

    [Fact]
    public void AuditedEntityWithKey_DefaultConstructor_InitializesWithDefaultId()
    {
        // Arrange & Act
        var entity = new TestAuditedEntityIntKey();

        // Assert
        entity.Id.Should().Be(default(int));
        entity.DateCreated.Should().BeNull();
        entity.CreatedBy.Should().BeNull();
        entity.DateLastModified.Should().BeNull();
        entity.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void AuditedEntityWithGuidKey_DefaultConstructor_InitializesWithDefaultId()
    {
        // Arrange & Act
        var entity = new TestAuditedEntityGuidKey();

        // Assert
        entity.Id.Should().Be(default(Guid));
    }

    [Fact]
    public void AuditedEntityWithKey_DateCreated_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();
        var expectedDate = _faker.Date.Past();

        // Act
        entity.DateCreated = expectedDate;

        // Assert
        entity.DateCreated.Should().Be(expectedDate);
    }

    [Fact]
    public void AuditedEntityWithKey_CreatedBy_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();
        var expectedUser = _faker.Internet.UserName();

        // Act
        entity.CreatedBy = expectedUser;

        // Assert
        entity.CreatedBy.Should().Be(expectedUser);
    }

    [Fact]
    public void AuditedEntityWithKey_DateLastModified_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();
        var expectedDate = _faker.Date.Recent();

        // Act
        entity.DateLastModified = expectedDate;

        // Assert
        entity.DateLastModified.Should().Be(expectedDate);
    }

    [Fact]
    public void AuditedEntityWithKey_LastModifiedBy_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();
        var expectedUser = _faker.Internet.UserName();

        // Act
        entity.LastModifiedBy = expectedUser;

        // Assert
        entity.LastModifiedBy.Should().Be(expectedUser);
    }

    [Fact]
    public void AuditedEntityWithKey_ImplementsIBusinessEntityWithKey()
    {
        // Arrange & Act
        var entity = new TestAuditedEntityIntKey();

        // Assert
        entity.Should().BeAssignableTo<IBusinessEntity<int>>();
    }

    [Fact]
    public void AuditedEntityWithKey_ImplementsIAuditedEntityWithKey()
    {
        // Arrange & Act
        var entity = new TestAuditedEntityIntKey();

        // Assert
        entity.Should().BeAssignableTo<IAuditedEntity<int, string, string>>();
    }

    [Fact]
    public void AuditedEntityWithKey_GetKeys_ReturnsIdInArray()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();

        // Act
        var keys = entity.GetKeys();

        // Assert
        keys.Should().HaveCount(1);
        keys[0].Should().Be(entity.Id);
    }

    #endregion

    #region Various User Type Tests

    [Fact]
    public void AuditedEntity_WithIntUsers_CanSetAndRetrieveUserIds()
    {
        // Arrange
        var entity = new TestAuditedEntityWithIntUsers();
        var createdByUserId = _faker.Random.Int(1, 1000);
        var modifiedByUserId = _faker.Random.Int(1, 1000);

        // Act
        entity.CreatedBy = createdByUserId;
        entity.LastModifiedBy = modifiedByUserId;

        // Assert
        entity.CreatedBy.Should().Be(createdByUserId);
        entity.LastModifiedBy.Should().Be(modifiedByUserId);
    }

    [Fact]
    public void AuditedEntity_WithMixedUsers_CanSetAndRetrieveValues()
    {
        // Arrange
        var entity = new TestAuditedEntityMixedUsers();
        var createdByUsername = _faker.Internet.UserName();
        var modifiedByGuid = _faker.Random.Guid();

        // Act
        entity.CreatedBy = createdByUsername;
        entity.LastModifiedBy = modifiedByGuid;

        // Assert
        entity.CreatedBy.Should().Be(createdByUsername);
        entity.LastModifiedBy.Should().Be(modifiedByGuid);
    }

    [Fact]
    public void AuditedEntity_WithComplexUsers_CanSetAndRetrieveUserObjects()
    {
        // Arrange
        var entity = new TestAuditedEntityWithComplexUsers();
        var createdByUser = new TestUser
        {
            UserId = _faker.Random.Int(1, 1000),
            Username = _faker.Internet.UserName()
        };
        var modifiedByUser = new TestUser
        {
            UserId = _faker.Random.Int(1, 1000),
            Username = _faker.Internet.UserName()
        };

        // Act
        entity.CreatedBy = createdByUser;
        entity.LastModifiedBy = modifiedByUser;

        // Assert
        entity.CreatedBy.Should().Be(createdByUser);
        entity.LastModifiedBy.Should().Be(modifiedByUser);
        entity.CreatedBy!.UserId.Should().Be(createdByUser.UserId);
        entity.LastModifiedBy!.Username.Should().Be(modifiedByUser.Username);
    }

    #endregion

    #region Audit Trail Simulation Tests

    [Fact]
    public void AuditedEntity_SimulateCreateAudit_SetsCreationInfo()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey { Name = _faker.Commerce.ProductName() };
        var createdBy = _faker.Internet.UserName();
        var createdDate = DateTime.UtcNow;

        // Act
        entity.DateCreated = createdDate;
        entity.CreatedBy = createdBy;

        // Assert
        entity.DateCreated.Should().Be(createdDate);
        entity.CreatedBy.Should().Be(createdBy);
        entity.DateLastModified.Should().BeNull();
        entity.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void AuditedEntity_SimulateUpdateAudit_SetsModificationInfo()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey { Name = _faker.Commerce.ProductName() };
        var createdBy = _faker.Internet.UserName();
        var createdDate = _faker.Date.Past();
        var modifiedBy = _faker.Internet.UserName();
        var modifiedDate = DateTime.UtcNow;

        // Act - Simulate creation
        entity.DateCreated = createdDate;
        entity.CreatedBy = createdBy;

        // Act - Simulate update
        entity.DateLastModified = modifiedDate;
        entity.LastModifiedBy = modifiedBy;

        // Assert
        entity.DateCreated.Should().Be(createdDate);
        entity.CreatedBy.Should().Be(createdBy);
        entity.DateLastModified.Should().Be(modifiedDate);
        entity.LastModifiedBy.Should().Be(modifiedBy);
    }

    [Theory]
    [InlineData("user1", "user2")]
    [InlineData("admin", "admin")]
    [InlineData("system", "user")]
    public void AuditedEntity_DifferentCreatorAndModifier_TracksCorrectly(string createdBy, string modifiedBy)
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();

        // Act
        entity.CreatedBy = createdBy;
        entity.LastModifiedBy = modifiedBy;

        // Assert
        entity.CreatedBy.Should().Be(createdBy);
        entity.LastModifiedBy.Should().Be(modifiedBy);
    }

    #endregion

    #region Inherited BusinessEntity Behavior Tests

    [Fact]
    public void AuditedEntity_InheritsLocalEventsFromBusinessEntity()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();

        // Assert
        entity.LocalEvents.Should().NotBeNull();
        entity.LocalEvents.Should().BeEmpty();
    }

    [Fact]
    public void AuditedEntity_CanAddLocalEvents()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };

        // Act
        entity.AddLocalEvent(testEvent);

        // Assert
        entity.LocalEvents.Should().HaveCount(1);
        entity.LocalEvents.Should().Contain(testEvent);
    }

    [Fact]
    public void AuditedEntity_InheritsAllowEventTracking()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();

        // Assert
        entity.AllowEventTracking.Should().BeTrue();
    }

    [Fact]
    public void AuditedEntity_ToString_ContainsEntityTypeAndId()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Contain("ENTITY:");
        result.Should().Contain("TestAuditedEntityIntKey");
    }

    #endregion

    #region Nullable DateTime Tests

    [Fact]
    public void AuditedEntity_DateCreated_CanBeSetToNull()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();
        entity.DateCreated = DateTime.UtcNow;

        // Act
        entity.DateCreated = null;

        // Assert
        entity.DateCreated.Should().BeNull();
    }

    [Fact]
    public void AuditedEntity_DateLastModified_CanBeSetToNull()
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();
        entity.DateLastModified = DateTime.UtcNow;

        // Act
        entity.DateLastModified = null;

        // Assert
        entity.DateLastModified.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetDateTimeTestData))]
    public void AuditedEntity_DateCreated_HandlesVariousDates(DateTime? testDate)
    {
        // Arrange
        var entity = new TestAuditedEntityIntKey();

        // Act
        entity.DateCreated = testDate;

        // Assert
        entity.DateCreated.Should().Be(testDate);
    }

    public static IEnumerable<object?[]> GetDateTimeTestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { DateTime.MinValue };
        yield return new object?[] { DateTime.MaxValue };
        yield return new object?[] { DateTime.UtcNow };
        yield return new object?[] { new DateTime(2000, 1, 1) };
    }

    #endregion
}
