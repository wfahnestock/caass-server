namespace CAASS.Auth.Models.Interfaces;

/// <summary>
/// ITenantEntity should be inherited by any entity "owned" by a specific tenant.
/// For example, every School, Student, Staff, etc. belongs to a specific tenant.
/// </summary>
public interface ITenantEntity
{
    public Guid TenantId { get; set; }
}