using CAASS.Core.Models.Schema;
using Microsoft.EntityFrameworkCore;

namespace CAASS.Core.Models.Context;

public class UserContext(DbContextOptions<UserContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}