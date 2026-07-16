using RCommon.Entities;

namespace Examples.DomainDrivenDesign;

public class TeamMemberAddedEvent : IDomainEvent
{
    public TeamMemberAddedEvent(Guid teamId, Guid userId, EmailAddress email)
    {
        TeamId = teamId;
        UserId = userId;
        Email = email;
    }

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    public Guid TeamId { get; }
    public Guid UserId { get; }
    public EmailAddress Email { get; }
}
