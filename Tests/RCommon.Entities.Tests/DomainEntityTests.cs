using Bogus;
using FluentAssertions;
using RCommon.Entities;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for DomainEntity{TKey} abstract class.
/// </summary>
public class DomainEntityTests
{
    private readonly Faker _faker;

    public DomainEntityTests()
    {
        _faker = new Faker();
    }

    #region Test Entities

    private class TestDomainEntityInt : DomainEntity<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestDomainEntityInt() { }

        public TestDomainEntityInt(int id)
        {
            Id = id;
        }
    }

    private class TestDomainEntityGuid : DomainEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestDomainEntityGuid() { }

        public TestDomainEntityGuid(Guid id)
        {
            Id = id;
        }
    }

    private class TestDomainEntityString : DomainEntity<string>
    {
        public string Name { get; set; } = string.Empty;

        public TestDomainEntityString() { }

        public TestDomainEntityString(string id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// A different entity type with the same key type, for cross-type equality tests.
    /// </summary>
    private class TestOtherDomainEntityInt : DomainEntity<int>
    {
        public TestOtherDomainEntityInt(int id)
        {
            Id = id;
        }
    }

    #endregion

    #region Identity Tests

    [Fact]
    public void DomainEntity_DefaultConstructor_IdIsDefault()
    {
        var entity = new TestDomainEntityInt();
        entity.Id.Should().Be(default(int));
    }

    [Fact]
    public void DomainEntity_ConstructorWithId_SetsId()
    {
        var id = _faker.Random.Int(1, 1000);
        var entity = new TestDomainEntityInt(id);
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void DomainEntity_GuidKey_SetsId()
    {
        var id = Guid.NewGuid();
        var entity = new TestDomainEntityGuid(id);
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void DomainEntity_StringKey_SetsId()
    {
        var id = _faker.Random.AlphaNumeric(10);
        var entity = new TestDomainEntityString(id);
        entity.Id.Should().Be(id);
    }

    #endregion

    #region Transient Detection Tests

    [Fact]
    public void IsTransient_DefaultIntId_ReturnsTrue()
    {
        var entity = new TestDomainEntityInt();
        entity.IsTransient().Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DefaultGuidId_ReturnsTrue()
    {
        var entity = new TestDomainEntityGuid();
        entity.IsTransient().Should().BeTrue();
    }

    [Fact]
    public void IsTransient_NullStringId_ReturnsTrue()
    {
        var entity = new TestDomainEntityString();
        entity.IsTransient().Should().BeTrue();
    }

    [Fact]
    public void IsTransient_NonDefaultId_ReturnsFalse()
    {
        var entity = new TestDomainEntityInt(42);
        entity.IsTransient().Should().BeFalse();
    }

    [Fact]
    public void IsTransient_NonEmptyGuidId_ReturnsFalse()
    {
        var entity = new TestDomainEntityGuid(Guid.NewGuid());
        entity.IsTransient().Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);
        entity1.Equals(entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(99);
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var entity = new TestDomainEntityInt(42);
        entity.Equals(entity).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var entity = new TestDomainEntityInt(42);
        entity.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_SameId_ReturnsFalse()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestOtherDomainEntityInt(42);
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_BothTransient_ReturnsFalse()
    {
        var entity1 = new TestDomainEntityInt();
        var entity2 = new TestDomainEntityInt();
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_OneTransient_ReturnsFalse()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt();
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectOverload_WorksCorrectly()
    {
        var entity1 = new TestDomainEntityInt(42);
        object entity2 = new TestDomainEntityInt(42);
        entity1.Equals(entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_NonDomainEntityObject_ReturnsFalse()
    {
        var entity = new TestDomainEntityInt(42);
        var nonEntity = "not an entity";
        entity.Equals(nonEntity).Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHash()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_TransientEntity_ReturnsObjectHashCode()
    {
        var entity1 = new TestDomainEntityInt();
        var entity2 = new TestDomainEntityInt();
        entity1.GetHashCode().Should().NotBe(0);
        entity2.GetHashCode().Should().NotBe(0);
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void EqualityOperator_SameId_ReturnsTrue()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_DifferentId_ReturnsFalse()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(99);
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        TestDomainEntityInt? entity1 = null;
        TestDomainEntityInt? entity2 = null;
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        var entity1 = new TestDomainEntityInt(42);
        TestDomainEntityInt? entity2 = null;
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_DifferentId_ReturnsTrue()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(99);
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_SameId_ReturnsFalse()
    {
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);
        (entity1 != entity2).Should().BeFalse();
    }

    #endregion

    #region IEquatable Tests

    [Fact]
    public void DomainEntity_Implements_IEquatable()
    {
        var entity = new TestDomainEntityInt(42);
        entity.Should().BeAssignableTo<IEquatable<DomainEntity<int>>>();
    }

    #endregion

    #region Does NOT implement IBusinessEntity

    [Fact]
    public void DomainEntity_DoesNotImplement_IBusinessEntity()
    {
        var entity = new TestDomainEntityInt(42);
        entity.Should().NotBeAssignableTo<IBusinessEntity>();
    }

    #endregion
}
