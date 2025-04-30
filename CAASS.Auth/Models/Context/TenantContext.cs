using CAASS.Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CAASS.Auth.Models.Context;

public class TenantContext(DbContextOptions<TenantContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<TenantContact> TenantContacts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}