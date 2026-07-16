using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;

namespace Examples.DomainDrivenDesign;

public class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Team> Teams { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(team =>
        {
            team.HasMany(t => t.Memberships)
                .WithOne()
                .HasForeignKey(m => m.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMembership>(membership =>
        {
            // EmailAddress is a single-value wrapper (RCommon.Entities.ValueObject<string>); store it
            // as its underlying string via a value conversion rather than an owned type.
            membership.Property(m => m.Email)
                .HasConversion(email => email.Value, value => EmailAddress.Create(value));
        });
    }
}
