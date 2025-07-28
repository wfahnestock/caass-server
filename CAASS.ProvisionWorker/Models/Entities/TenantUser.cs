namespace CAASS.ProvisionWorker.Models.Entities;

public record TenantUser
{
    public required int TenantUserId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int RetryCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? LockedReason { get; set; } = null;
}

public enum LockedReason
{
    PasswordRetryCountExceeded = 1,
    PasswordChangeRequired = 2,
    LockedByAdmin = 3,
    
}