using Microsoft.EntityFrameworkCore;
using Properties.Domain;

namespace Properties.Infrastructure;

public class PropertiesDbContext : DbContext
{
    public PropertiesDbContext(DbContextOptions<PropertiesDbContext> options)
        : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Plot> Plots => Set<Plot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(256);
            e.Property(p => p.Location).HasMaxLength(512);
            e.HasMany(p => p.Plots).WithOne(pl => pl.Property).HasForeignKey(pl => pl.PropertyId);
        });
        modelBuilder.Entity<Plot>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(256);
            e.Property(p => p.Culture).HasMaxLength(256);
        });
    }
}
