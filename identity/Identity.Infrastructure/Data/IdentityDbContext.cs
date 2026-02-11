using Microsoft.EntityFrameworkCore;
using Identity.Domain;

namespace Identity.Infrastructure.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    public DbSet<Producer> Producers => Set<Producer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producer>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Email).IsUnique();
            e.Property(p => p.Email).HasMaxLength(256);
            e.Property(p => p.PasswordHash).HasMaxLength(256);
        });
    }
}
