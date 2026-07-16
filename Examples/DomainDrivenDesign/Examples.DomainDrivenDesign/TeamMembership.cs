using RCommon.Entities;

namespace Examples.DomainDrivenDesign;

/// <summary>
/// A child entity of the <see cref="Team"/> aggregate -- not an aggregate root itself, so it derives
/// from <see cref="BusinessEntity{TKey}"/> rather than <see cref="AggregateRoot{TKey}"/>. Persisted
/// only ever through Team's own IAggregateRepository (Pattern 1) or, for cross-scope mutation, through
/// its own repository (Pattern 2) -- see persistence/aggregate-repository.mdx.
/// </summary>
public class TeamMembership : BusinessEntity<Guid>
{
    public TeamMembership(Guid teamId, Guid userId, EmailAddress email, TeamRole role)
        : base(Guid.NewGuid())
    {
        TeamId = teamId;
        UserId = userId;
        Email = email;
        Role = role;
    }

    private TeamMembership()
    {
        // Required by EF Core for materialization; Email is assigned via the backing field below.
        Email = null!;
    }

    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public EmailAddress Email { get; private set; }
    public TeamRole Role { get; private set; }
}
