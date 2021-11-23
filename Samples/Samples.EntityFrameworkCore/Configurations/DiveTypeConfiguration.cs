

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samples.Domain.Entities;

namespace Samples.ObjectAccess.EFCore
{

    // DiveTypes

    public partial class DiveTypeConfiguration : IEntityTypeConfiguration<DiveType>

    {


        public void Configure(EntityTypeBuilder<DiveType> builder)

        {
            builder.ToTable("DiveTypes", "dbo");
            builder.HasKey(x => x.Id).HasName("PK_DiveTypes").IsClustered();
            builder.Ignore(x => x.AllowChangeTracking);
            builder.Ignore(x => x.IsChanged);
            builder.Property(x => x.Id).HasColumnName(@"Id").HasColumnType("uniqueidentifier").IsRequired().ValueGeneratedNever();
            builder.Property(x => x.DiveTypeName).HasColumnName(@"DiveTypeName").HasColumnType("nvarchar(50)").IsRequired().HasMaxLength(50);
            builder.Property(x => x.DiveTypeDesc).HasColumnName(@"DiveTypeDesc").HasColumnType("ntext").IsRequired();
        }

    }

}

