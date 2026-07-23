using RCommon.EventHandling.Subscribers;

namespace Examples.EventHandling.NoUnitOfWork;

/// <summary>
/// Subscriber for <see cref="NotificationRequested"/>. Exposes a static <see cref="HandledCount"/>
/// so the end-to-end test can assert (delta-based) that the handler actually ran when the event was
/// published directly through the <c>IEventBus</c> singleton, with no UnitOfWork involved.
/// </summary>
public class NotificationRequestedHandler : ISubscriber<NotificationRequested>
{
    public static int HandledCount;

    public Task HandleAsync(NotificationRequested @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref HandledCount);
        Console.WriteLine($"Notification for {@event.RecipientId}: {@event.Message}");
        return Task.CompletedTask;
    }
}
