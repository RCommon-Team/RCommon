using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RCommon.Persistence.Inbox;

namespace RCommon.Persistence.EFCore.Inbox;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    private readonly string _tableName;

    public InboxMessageConfiguration(string tableName = "__InboxMessages")
    {
        _tableName = tableName;
    }

    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable(_tableName);
        builder.HasKey(x => new { x.MessageId, x.ConsumerType });
        builder.Property(x => x.ConsumerType)
            .HasMaxLength(512)
            .HasDefaultValue("")
            .IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ReceivedAtUtc).IsRequired();

        builder.HasIndex(x => x.ReceivedAtUtc)
            .HasDatabaseName("IX_InboxMessages_Cleanup");
    }
}
