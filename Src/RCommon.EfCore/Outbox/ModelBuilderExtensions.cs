using Microsoft.EntityFrameworkCore;

namespace RCommon.Persistence.EFCore.Outbox;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddOutboxMessages(this ModelBuilder modelBuilder, string tableName = "__OutboxMessages")
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(tableName));
        return modelBuilder;
    }
}
