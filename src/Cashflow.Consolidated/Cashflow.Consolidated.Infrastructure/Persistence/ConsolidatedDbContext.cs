using Cashflow.Consolidated.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cashflow.Consolidated.Infrastructure.Persistence;

public class ConsolidatedDbContext : DbContext
{
    public DbSet<DailyBalance> DailyBalances { get; set; }

    public ConsolidatedDbContext(DbContextOptions<ConsolidatedDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyBalance>().HasKey(b => b.Id);
        modelBuilder.Entity<DailyBalance>().HasIndex(b => b.Date).IsUnique();
        modelBuilder.Entity<DailyBalance>().Property(b => b.Balance).HasColumnType("NUMERIC(18,2)");
    }
}