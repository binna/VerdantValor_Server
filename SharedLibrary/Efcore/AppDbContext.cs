using Microsoft.EntityFrameworkCore;
using Efcore.Converters;
using Common.Models;
using Common.Types;

namespace Efcore;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GameUser> GameUser { get; set; } = null!;
    public DbSet<Purchase> Purchase { get; set; } = null!;
    public DbSet<Inventory> Inventory { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<GameUser>(entity =>
            {
                entity.HasKey(e => e.UserId);
            })
            .Entity<Purchase>(entity =>
            {
                entity.HasKey(table => table.Id);
                entity.HasIndex(table => new { table.UserId, table.StoreId }).IsUnique();
            })
            .Entity<Inventory>(entity =>
            {
                entity.HasKey(table => table.Id);
                entity.HasIndex(table => new { table.UserId, table.ItemId }).IsUnique();
            })
            ;
    }

     protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
     {
         configurationBuilder
             .Properties<ServerDateTime>().HaveConversion<ServerDateTimeConverter>();
     }
}