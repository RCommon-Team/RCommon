
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samples.Domain.Entities;

namespace Samples.ObjectAccess.EFCore
{

    // DiveLocationDetails

    public partial class DiveLocationDetailConfiguration : IEntityTypeConfiguration<DiveLocationDetail>

    {

        public void Configure(EntityTypeBuilder<DiveLocationDetail> builder)

        {
            builder.ToTable("DiveLocationDetails", "dbo");
            builder.HasKey(x => x.DiveLocationId).HasName("PK_DiveLocationDetails").IsClustered();
            builder.Ignore(x => x.AllowChangeTracking);
            builder.Ignore(x => x.IsChanged);
            builder.Property(x => x.DiveLocationId).HasColumnName(@"DiveLocationId").HasColumnType("uniqueidentifier").IsRequired().ValueGeneratedNever();
            builder.Property(x => x.ImageData).HasColumnName(@"ImageData").HasColumnType("varbinary(max)").IsRequired(false);

        }

    }

}

