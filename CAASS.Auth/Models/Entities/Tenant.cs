namespace CAASS.Auth.Models.Entities;

/// <summary>
/// The Tenant entity holds information about each customer.
/// <param name="TenantId">Tenant's Unique ID</param>
/// <param name="Email">Tenant's Email address</param>
/// <param name="Password">Tenant's password</param>
/// <param name="CreatedAt">DateTime when the Tenant was created</param>
/// <param name="LastLogin">DateTime when the Tenant last logged in</param>
/// <param name="RetryCount">Amount of times a login was attempted</param>
/// <param name="IsActive">Is this Tenant active or dormant?</param>
/// <param name="IsLocked">Is this Tenant account locked?</param>
/// <param name="LockedReason">The reason for the Tenant account being locked. Could be due to too many login retries, billing related, etc.</param>
/// <param name="OrganizationName">The Tenant's Organization, typically the School District they belong to.</param>
/// <param name="OrganizationEmailDomain">The Tenant's email domain. Used for Tenant lookup upon User login.</param>
/// <param name="TenantSlug">The Slug used to build this Tenant's instanced database.</param>
/// <param name="RefreshToken">The currently valid Refresh Token for this Tenant</param>
/// <param name="RefreshTokenExpiryDateTime">When does Tenant's Refresh Token expire?</param>
/// <seealso cref="TenantContact">TenantContact - For details about contact information for a Tenant.</seealso>
/// </summary>
public record Tenant
{
    public Guid TenantId { get; init; }
    public required string Email { get; init; }
    public required string Password { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int RetryCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? LockedReason { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public required string OrganizationEmailDomain { get; set; } = string.Empty;
    public required string TenantSlug { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryDateTime { get; set; }
}