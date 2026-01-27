using Microsoft.EntityFrameworkCore;
using Efcore.Converter;
using Common.Models;
using Common.Types;

namespace Efcore;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Users> Users { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Users>()
            .HasKey(u => u.UserId);
    }

     protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
     {
         configurationBuilder
             .Properties<ServerDateTime>().HaveConversion<ServerDateTimeConverter>();
     }
}