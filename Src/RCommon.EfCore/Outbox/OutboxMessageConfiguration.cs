using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    private readonly string _tableName;

    public OutboxMessageConfiguration(string tableName = "__OutboxMessages")
    {
        _tableName = tableName;
    }

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(_tableName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.EventPayload).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(256);
        builder.Property(x => x.TenantId).HasMaxLength(256);
        builder.Property(x => x.NextRetryAtUtc);
        builder.Property(x => x.LockedByInstanceId).HasMaxLength(64);
        builder.Property(x => x.LockedUntilUtc);

        builder.HasIndex(x => new { x.ProcessedAtUtc, x.DeadLetteredAtUtc, x.NextRetryAtUtc, x.LockedUntilUtc, x.CreatedAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Pending");

        builder.HasIndex(x => x.DeadLetteredAtUtc)
            .HasDatabaseName("IX_OutboxMessages_DeadLettered")
            .HasFilter("[DeadLetteredAtUtc] IS NOT NULL");
    }
}
