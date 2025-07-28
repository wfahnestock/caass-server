using System.Diagnostics.CodeAnalysis;
using CAASS.ProvisionWorker.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CAASS.ProvisionWorker.Models.Context;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public class TenantDbContext: DbContext
{
    private readonly string? _connectionString;
    public TenantDbContext(string connectionSting) : base(GetOptions(connectionSting))
    {
        _connectionString = connectionSting;
    } 
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }
    
    public DbSet<TenantUser> TenantUsers { get; set; } = null!;
    public DbSet<TenantUserRole> TenantUserRoles { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=bluesky1!");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TenantUser>(entity =>
        {
            entity.HasKey(e => e.TenantUserId);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.LastLogin).HasDefaultValueSql(null);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.LockedReason).HasDefaultValue(string.Empty);
        });

        modelBuilder.Entity<TenantUserRole>(entity =>
        {
            entity.HasData(
                new TenantUserRole
                {
                    TenantUserRoleId = Guid.Parse("0196a16f-d04e-7019-89c3-2f189a312d8f"),
                    RoleName = "System Administrator", 
                    RoleDescription = "CAASS System Administrator",
                    IsSystemDefined = true
                },
                new TenantUserRole
                {
                    TenantUserRoleId = Guid.Parse("0196a16f-d04e-742d-a134-565100389fb6"),
                    RoleName = "Administrator", 
                    RoleDescription = "CAASS Administrator",
                    IsSystemDefined = true
                }
            );
        });
    }

    private static DbContextOptions GetOptions(string connectionString)
    {
        return new DbContextOptionsBuilder().UseNpgsql(connectionString).Options;
    }
}