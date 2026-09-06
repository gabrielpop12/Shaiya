using Microsoft.EntityFrameworkCore;
using Shaiya2.Server.Persistence.Entities;

namespace Shaiya2.Server.Persistence;

public sealed class ShaiyaDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Character> Characters => Set<Character>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        string? connectionString =
            Environment.GetEnvironmentVariable(
                "SHAIYA2_DB_CONNECTION"
            );

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Environment variable SHAIYA2_DB_CONNECTION is not configured."
            );
        }

        optionsBuilder.UseNpgsql(
            connectionString
        );
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(
            entity =>
            {
                entity.ToTable("characters");

                entity.HasKey(
                    character => character.Id
                );

                entity.Property(
                        character => character.Name)
                    .HasMaxLength(24)
                    .IsRequired();

                entity.Property(
                        character => character.NormalizedName)
                    .HasMaxLength(24)
                    .IsRequired();

                entity.HasIndex(
                        character => character.NormalizedName)
                    .IsUnique();

                entity.Property(
                        character => character.Faction)
                    .IsRequired();

                entity.Property(
                        character => character.Class)
                    .IsRequired();

                entity.Property(
                        character => character.Gender)
                    .IsRequired();

                entity.Property(
                        character => character.Level)
                    .IsRequired();

                entity.Property(
                        character => character.Experience)
                    .IsRequired();

                entity.Property(
                        character => character.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(
                        character => character.Account)
                    .WithMany(
                        account => account.Characters)
                    .HasForeignKey(
                        character => character.AccountId)
                    .OnDelete(
                        DeleteBehavior.Cascade
                    );
            }
        );
    }


}