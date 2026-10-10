using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Infrastructure.Persistence;

public sealed class HuntAnalyzerDbContext(DbContextOptions<HuntAnalyzerDbContext> options)
    : DbContext(options)
{
    public DbSet<Character> Characters => Set<Character>();

    public DbSet<HuntLocation> HuntLocations => Set<HuntLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(character => character.Id);
            entity.Property(character => character.Name).IsRequired();
            entity.Property(character => character.IsActive).IsRequired();
        });

        modelBuilder.Entity<HuntLocation>(entity =>
        {
            entity.HasKey(location => location.Id);
            entity.Property(location => location.Name).IsRequired();
            entity.Property(location => location.IsActive).IsRequired();
        });
    }
}
