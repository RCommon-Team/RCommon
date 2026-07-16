using RCommon.Entities;

namespace Examples.DomainDrivenDesign;

/// <summary>
/// The aggregate root. Derives from AggregateRoot&lt;TKey&gt; (domain events, versioning),
/// implements ISoftDelete (DeleteAsync marks IsDeleted rather than physically removing the row) and
/// IMultiTenant (AddAsync stamps TenantId automatically -- kept minimal here; see
/// Examples.MultiTenancy.Finbuckle for TenantScope.Bypass() and Finbuckle-specific wiring).
/// </summary>
public class Team : AggregateRoot<Guid>, ISoftDelete, IMultiTenant
{
    private readonly List<TeamMembership> _memberships = new();

    public Team(string name) : base(Guid.NewGuid())
    {
        Name = name;
    }

    private Team()
    {
        // Required by EF Core for materialization.
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public ICollection<TeamMembership> Memberships => _memberships;

    public bool IsDeleted { get; set; }

    public string? TenantId { get; set; }

    /// <summary>
    /// Adds a member and raises <see cref="TeamMemberAddedEvent"/>. The event is only dispatched
    /// once the caller persists this aggregate through IAggregateRepository and commits a UnitOfWork
    /// -- see domain-driven-design/domain-events.mdx.
    /// </summary>
    public void AddMember(Guid userId, EmailAddress email, TeamRole role)
    {
        var membership = new TeamMembership(Id, userId, email, role);
        _memberships.Add(membership);

        AddDomainEvent(new TeamMemberAddedEvent(Id, userId, email));
    }
}
