using RCommon.EventHandling.Subscribers;

namespace Examples.DomainDrivenDesign;

public class TeamMemberAddedEventHandler : ISubscriber<TeamMemberAddedEvent>
{
    public static int HandledCount { get; private set; }

    public Task HandleAsync(TeamMemberAddedEvent @event, CancellationToken cancellationToken = default)
    {
        HandledCount++;
        Console.WriteLine($"  [subscriber] Welcome email queued for {@event.Email} (team {@event.TeamId})");
        return Task.CompletedTask;
    }
}
