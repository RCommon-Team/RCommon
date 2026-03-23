using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence.Inbox;

namespace RCommon;

public static class InboxPersistenceBuilderExtensions
{
    public static IPersistenceBuilder AddInbox<TInboxStore>(this IPersistenceBuilder builder)
        where TInboxStore : class, IInboxStore
    {
        builder.Services.AddScoped<IInboxStore, TInboxStore>();
        return builder;
    }
}
