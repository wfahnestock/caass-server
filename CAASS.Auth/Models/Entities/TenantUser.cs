using CAASS.Auth.Models.Interfaces;

namespace CAASS.Auth.Models.Entities;

public record TenantUser : ITenantEntity
{
    public required int UserId { get; set; }
    public required Guid TenantId { get; set; }
    public Tenant Tenant { get; init; } = null!; // Required reference navigation to principal entity (Tenant)
    public required string Email { get; set; }
    public required string Password { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int RetryCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string LockedReason { get; set; } = string.Empty;
}

