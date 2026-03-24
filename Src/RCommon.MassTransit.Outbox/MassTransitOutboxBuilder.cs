using MassTransit;

namespace RCommon.MassTransit.Outbox;

public class MassTransitOutboxBuilder : IMassTransitOutboxBuilder
{
    private readonly IEntityFrameworkOutboxConfigurator _configurator;

    public MassTransitOutboxBuilder(IEntityFrameworkOutboxConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
    }

    public IMassTransitOutboxBuilder UsePostgres()
    {
        _configurator.UsePostgres();
        return this;
    }

    public IMassTransitOutboxBuilder UseSqlServer()
    {
        _configurator.UseSqlServer();
        return this;
    }

    public IMassTransitOutboxBuilder UseBusOutbox(Action<IBusOutboxConfigurator>? configure = null)
    {
        _configurator.UseBusOutbox(configure);
        return this;
    }
}
