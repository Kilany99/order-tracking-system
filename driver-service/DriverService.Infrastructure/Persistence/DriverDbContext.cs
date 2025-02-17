using DriverService.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace DriverService.Infrastructure.Persistence;
public class DriverDbContext : DbContext
{
    public DriverDbContext(DbContextOptions<DriverDbContext> options)
        : base(options) { }

    public DbSet<Driver> Drivers { get; set; }
    public DbSet<DriverAuth> DriverAuths { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure DriverAuth (one-to-one with Driver)
        modelBuilder.Entity<DriverAuth>(entity =>
        {
            entity.HasKey(da => da.DriverId); // Primary key

            entity.HasOne(da => da.Driver)
                .WithOne(d => d.Auth)
                .HasForeignKey<DriverAuth>(da => da.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RefreshToken (many-to-one with Driver)
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id); // Primary key

            entity.HasIndex(rt => rt.Token)
                .IsUnique();

            entity.HasOne(rt => rt.Driver)
                .WithMany(d => d.RefreshTokens)
                .HasForeignKey(rt => rt.DriverId);
        });

        // Configure Driver
        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(d => d.Id); // Primary key
            entity.HasIndex(d => d.IsAvailable);
        });
    }
}

