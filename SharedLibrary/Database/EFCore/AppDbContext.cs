using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace SharedLibrary.Database.EFCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Users> Users { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Users>()
            .HasKey(u => u.UserId);
    }
}