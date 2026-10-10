using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Infrastructure.Persistence;

public sealed class HuntAnalyzerDbContext(DbContextOptions<HuntAnalyzerDbContext> options)
    : DbContext(options)
{
    public DbSet<Character> Characters => Set<Character>();

    public DbSet<HuntLocation> HuntLocations => Set<HuntLocation>();

    public DbSet<HuntSession> HuntSessions => Set<HuntSession>();

    public DbSet<KilledMonster> KilledMonsters => Set<KilledMonster>();

    public DbSet<LootedItem> LootedItems => Set<LootedItem>();

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

        modelBuilder.Entity<HuntSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.Property(session => session.CharacterLevel).IsRequired();
            entity.Property(session => session.SessionStart).IsRequired();
            entity.Property(session => session.SessionEnd).IsRequired();
            entity.Property(session => session.Duration)
                .HasConversion(duration => duration.Ticks, ticks => TimeSpan.FromTicks(ticks))
                .IsRequired();
            entity.Property(session => session.InputFormat).IsRequired();
            entity.Property(session => session.OriginalInput).IsRequired();

            entity.HasOne(session => session.Character)
                .WithMany()
                .HasForeignKey(session => session.CharacterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(session => session.HuntLocation)
                .WithMany()
                .HasForeignKey(session => session.HuntLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.OwnsOne(session => session.Statistics, statistics =>
            {
                statistics.Property(value => value.Balance);
                statistics.Property(value => value.Damage);
                statistics.Property(value => value.DamagePerHour);
                statistics.Property(value => value.Healing);
                statistics.Property(value => value.HealingPerHour);
                statistics.Property(value => value.Loot);
                statistics.Property(value => value.Supplies);
                statistics.Property(value => value.XpGain);
                statistics.Property(value => value.XpPerHour);
                statistics.Property(value => value.RawXpGain);
                statistics.Property(value => value.RawXpPerHour);
            });

            entity.HasMany(session => session.KilledMonsters)
                .WithOne()
                .HasForeignKey(monster => monster.HuntSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(session => session.KilledMonsters)
                .HasField("_killedMonsters")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(session => session.LootedItems)
                .WithOne()
                .HasForeignKey(item => item.HuntSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(session => session.LootedItems)
                .HasField("_lootedItems")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<KilledMonster>(entity =>
        {
            entity.HasKey(monster => monster.Id);
            entity.Property(monster => monster.Name).IsRequired();
        });

        modelBuilder.Entity<LootedItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();
        });
    }
}
