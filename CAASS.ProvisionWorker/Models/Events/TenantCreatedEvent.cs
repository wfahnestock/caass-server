namespace CAASS.ProvisionWorker.Models.Events;

public class TenantCreatedEvent
{
    public Guid TenantId { get; set; }
    public string TenantSlug { get; set; } = string.Empty;
}