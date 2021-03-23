
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samples.Domain.Entities;

namespace Samples.ObjectAccess.EFCore
{

    // AspNetUsers

    public partial class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>

    {

        public void Configure(EntityTypeBuilder<ApplicationUser> builder)

        {


            builder.ToTable("Users", "dbo");
            builder.HasKey(x => x.Id).HasName("PK_Users").IsClustered();





            builder.Property(x => x.Id).HasColumnName(@"Id").HasColumnType("int").IsRequired().ValueGeneratedOnAdd().UseIdentityColumn();

            builder.Property(x => x.FirstName).HasColumnName(@"FirstName").HasColumnType("nvarchar(50)").IsRequired(false).HasMaxLength(50);
            builder.Property(x => x.LastName).HasColumnName(@"LastName").HasColumnType("nvarchar(50)").IsRequired(false).HasMaxLength(50);
        }




    }

}
// </auto-generated>

