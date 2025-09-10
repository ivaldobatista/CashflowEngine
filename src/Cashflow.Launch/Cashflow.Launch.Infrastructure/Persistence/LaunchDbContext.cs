using Cashflow.Launch.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cashflow.Launch.Infrastructure.Persistence;

public class LaunchDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }

    public LaunchDbContext(DbContextOptions<LaunchDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>().HasKey(t => t.Id);
        modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasColumnType("decimal(18, 2)");
    }
}