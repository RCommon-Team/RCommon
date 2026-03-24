using MassTransit;

namespace RCommon.MassTransit.Outbox;

public interface IMassTransitOutboxBuilder
{
    IMassTransitOutboxBuilder UsePostgres();
    IMassTransitOutboxBuilder UseSqlServer();
    IMassTransitOutboxBuilder UseBusOutbox(Action<IBusOutboxConfigurator>? configure = null);
}
