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
    public DbSet<ChatParty> ChatParty { get; set; } = null!;
    public DbSet<ChatPartyMember> ChatPartyMember { get; set; } = null!;
    public DbSet<ChatPartyInvitation> ChatPartyInvitation { get; set; } = null!;
    
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
            .Entity<ChatParty>(entity =>
            {
                entity.HasKey(table => table.PartyId);
            })
            .Entity<ChatPartyMember>(entity =>
            {
                entity.HasKey(table => new { table.PartyId, table.UserId });
                entity.HasIndex(table => table.PartyId).IsUnique();
            })
            .Entity<ChatPartyInvitation>(entity =>
            {
                entity.HasKey(table => new { table.PartyId, table.UserId });
                entity.HasIndex(table => table.PartyId).IsUnique();
            })
            ;
    }

     protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
     {
         configurationBuilder
             .Properties<ServerDateTime>().HaveConversion<ServerDateTimeConverter>();
     }
}