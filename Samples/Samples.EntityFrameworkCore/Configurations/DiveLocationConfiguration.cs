
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samples.Domain.Entities;

namespace Samples.ObjectAccess.EFCore
{

    // DiveLocations

    public partial class DiveLocationConfiguration : IEntityTypeConfiguration<DiveLocation>

    {

        public void Configure(EntityTypeBuilder<DiveLocation> builder)
        {
            builder.ToTable("DiveLocations", "dbo");
            builder.HasKey(x => x.Id).HasName("PK_DiveLocations").IsClustered();
            builder.Ignore(x => x.AllowChangeTracking);
            builder.Ignore(x => x.IsChanged);
            builder.Property(x => x.Id).HasColumnName(@"Id").HasColumnType("uniqueidentifier").IsRequired().ValueGeneratedNever();
            builder.Property(x => x.LocationName).HasColumnName(@"LocationName").HasColumnType("nvarchar(255)").IsRequired().HasMaxLength(255);
            builder.Property(x => x.GpsCoordinates).HasColumnName(@"GpsCoordinates").HasColumnType("nvarchar(255)").IsRequired().HasMaxLength(255);
            builder.Property(x => x.DiveTypeId).HasColumnName(@"DiveTypeId").HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(x => x.DiveDesc).HasColumnName(@"DiveDesc").HasColumnType("ntext").IsRequired();

            // Foreign keys
            builder.HasOne(a => a.DiveLocationDetail).WithOne(b => b.DiveLocation).HasForeignKey<DiveLocation>(c => c.Id).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DiveLocations_DiveLocationDetails");
            builder.HasOne(a => a.DiveType).WithMany(b => b.DiveLocations).HasForeignKey(c => c.DiveTypeId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DiveLocations_DiveTypes");

        }




    }

}

