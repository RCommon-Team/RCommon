using Examples.DomainDrivenDesign;
using FluentAssertions;
using Xunit;

namespace Examples.DomainDrivenDesign.Tests;

public class TeamTests
{
    [Fact]
    public void AddMember_RaisesTeamMemberAddedEvent_WithCorrectData()
    {
        var team = new Team("Platform Engineering");
        var userId = Guid.NewGuid();
        var email = EmailAddress.Create("ada@example.com");

        team.AddMember(userId, email, TeamRole.Lead);

        team.DomainEvents.Should().ContainSingle();
        var raised = team.DomainEvents.Single().Should().BeOfType<TeamMemberAddedEvent>().Subject;
        raised.TeamId.Should().Be(team.Id);
        raised.UserId.Should().Be(userId);
        raised.Email.Should().Be(email);
    }

    [Fact]
    public void AddMember_AddsMembershipToCollection()
    {
        var team = new Team("Platform Engineering");
        var userId = Guid.NewGuid();

        team.AddMember(userId, EmailAddress.Create("ada@example.com"), TeamRole.Lead);

        team.Memberships.Should().ContainSingle();
        var membership = team.Memberships.Single();
        membership.TeamId.Should().Be(team.Id);
        membership.UserId.Should().Be(userId);
        membership.Role.Should().Be(TeamRole.Lead);
    }

    [Fact]
    public void AddMember_CalledTwice_RaisesOneEventPerCall()
    {
        var team = new Team("Platform Engineering");

        team.AddMember(Guid.NewGuid(), EmailAddress.Create("ada@example.com"), TeamRole.Lead);
        team.AddMember(Guid.NewGuid(), EmailAddress.Create("grace@example.com"), TeamRole.Member);

        team.DomainEvents.Should().HaveCount(2);
        team.Memberships.Should().HaveCount(2);
    }

    [Fact]
    public void IsDeleted_DefaultsToFalse_AndCanBeSetTrue()
    {
        var team = new Team("Platform Engineering");

        team.IsDeleted.Should().BeFalse();

        team.IsDeleted = true;

        team.IsDeleted.Should().BeTrue();
    }
}

public class EmailAddressTests
{
    [Fact]
    public void Create_TwoInstancesWithSameValue_AreEqual()
    {
        var first = EmailAddress.Create("ada@example.com");
        var second = EmailAddress.Create("ada@example.com");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Create_TwoInstancesWithDifferentValue_AreNotEqual()
    {
        var first = EmailAddress.Create("ada@example.com");
        var second = EmailAddress.Create("grace@example.com");

        first.Should().NotBe(second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData(" ")]
    public void Create_InvalidValue_Throws(string value)
    {
        var act = () => EmailAddress.Create(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsUnderlyingValue()
    {
        var email = EmailAddress.Create("ada@example.com");

        string raw = email;

        raw.Should().Be("ada@example.com");
    }
}
