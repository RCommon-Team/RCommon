using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddOutboxMessages(this ModelBuilder modelBuilder, string tableName = "__OutboxMessages")
    {
        // Idempotent: mapping is a no-op if OutboxMessage is already part of the model. This lets the
        // base RCommonDbContext auto-map an outbox-owning datastore while a derived context can still call
        // this manually without producing a double-mapping.
        if (modelBuilder.Model.FindEntityType(typeof(OutboxMessage)) is not null)
        {
            return modelBuilder;
        }

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(tableName));
        return modelBuilder;
    }

    public static ModelBuilder AddInboxMessages(this ModelBuilder modelBuilder, string tableName = "__InboxMessages")
    {
        modelBuilder.ApplyConfiguration(new RCommon.Persistence.EFCore.Inbox.InboxMessageConfiguration(tableName));
        return modelBuilder;
    }
}
