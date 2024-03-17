using Bankai.MLApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Dataset = Bankai.MLApi.Data.Entities.Dataset;

namespace Bankai.MLApi.Data;

public class MLApiDbContext : DbContext
{
    public DbSet<Model> Models { get; set; }

    public DbSet<Dataset> Datasets { get; set; }

    public MLApiDbContext(DbContextOptions<MLApiDbContext> opts) : base(opts)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Model>()
            .HasIndex(m => m.Name)
            .IsUnique();
    }
}
