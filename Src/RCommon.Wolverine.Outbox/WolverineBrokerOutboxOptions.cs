namespace RCommon.Wolverine.Outbox;

/// <summary>
/// Mirror-shape options so a developer copying a MassTransit recipe-2b call compiles against the Wolverine
/// builder — but <see cref="WolverineOutboxBuilderExtensions.UseBrokerOutbox{TDbContext}"/> always throws
/// (recipe 2b is NO-GO for Wolverine), so these methods are never invoked.
/// </summary>
public sealed class WolverineBrokerOutboxOptions
{
    public WolverineBrokerOutboxOptions OnDataStore(string dataStoreName) => this;
    public WolverineBrokerOutboxOptions UsePostgres() => this;
    public WolverineBrokerOutboxOptions UseSqlServer() => this;
}
