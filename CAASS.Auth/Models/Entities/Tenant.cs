namespace CAASS.Auth.Models.Entities;

/// <summary>
/// The Tenant entity holds information about each customer.
/// <param name="Email"/>Tenant's Email address</param>
/// <param name="Password"/>Tenant's password</param>
/// <param name="CreatedAt"/>DateTime when the Tenant was created</param>
/// <param name="LastLogin"/>DateTime when the Tenant last logged in</param>
/// <param name="RetryCount"/>Amount of times a login was attempted</param>
/// <param name="IsActive"/>Is this Tenant active or dormant?</param>
/// <param name="IsLocked"/>Is this Tenant account locked?</param>
/// <param name="RefreshToken"/>The currently valid Refresh Token for this Tenant</param>
/// <param name="RefreshTokenExpiryDateTime"/>When does Tenant's Refresh Token expire?</param>
/// </summary>
public record Tenant
{
    public Guid TenantId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int RetryCount { get; set; } = 0;
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryDateTime { get; set; }
}