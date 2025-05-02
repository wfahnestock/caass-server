using System.ComponentModel.DataAnnotations;
using CAASS.Auth.Models.Interfaces;

namespace CAASS.Auth.Models.Entities;

public record TenantUserRole : ITenantEntity
{
    [Required]
    public required Guid TenantUserRoleId { get; init; }
    [Required]
    public required Guid TenantId { get; set; }
    [Required]
    public required Tenant Tenant { get; set; } = null!; // Required reference navigation to principal entity (Tenant)
    [Required]
    [MaxLength(50)]
    public required string RoleName { get; set; } = string.Empty;
    [Required]
    [MaxLength(256)]
    public required string RoleDescription { get; set; } = string.Empty;
    [Required]
    public bool IsSystemDefined { get; set; }
}