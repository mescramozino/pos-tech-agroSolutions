using Microsoft.EntityFrameworkCore;
using Analysis.Api.Entities;

namespace Analysis.Api.Data;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options)
        : base(options) { }

    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.Message).HasMaxLength(512);
            e.HasIndex(x => new { x.PlotId, x.CreatedAt });
        });
    }
}
