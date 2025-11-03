using Microsoft.EntityFrameworkCore;

namespace GhToFreshdesk.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Job>(e =>
        {
            e.ToTable("Jobs");
            e.HasKey(x => x.Id);

            e.Property(x => x.Tenant).IsRequired().HasMaxLength(200);
            e.Property(x => x.Login).IsRequired().HasMaxLength(200);

            e.Property(x => x.Status).HasConversion<string>().IsRequired();

            e.Property(x => x.LastError).HasMaxLength(4000);
            e.Property(x => x.CorrelationKey).IsRequired().HasMaxLength(500);

            e.Property(x => x.Version).IsConcurrencyToken();

            e.HasIndex(x => new { x.Status, x.AvailableAt });
            e.HasIndex(x => x.CorrelationKey).IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Job>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}