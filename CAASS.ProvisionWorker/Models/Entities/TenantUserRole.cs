using System.ComponentModel.DataAnnotations;

namespace CAASS.ProvisionWorker.Models.Entities;

public record TenantUserRole
{
    [Required]
    public required Guid TenantUserRoleId { get; init; }
    [Required]
    [MaxLength(50)]
    public required string RoleName { get; set; } = string.Empty;
    [Required]
    [MaxLength(256)]
    public required string RoleDescription { get; set; } = string.Empty;
    [Required]
    public bool IsSystemDefined { get; set; }
}