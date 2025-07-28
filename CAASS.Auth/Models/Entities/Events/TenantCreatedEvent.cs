using CAASS.Auth.Models.Api.Dto;

namespace CAASS.Auth.Models.Entities.Events;

public class TenantCreatedEvent
{
    public required Guid TenantId { get;set; }
    public required string TenantSlug { get; set; }
    public IEnumerable<TenantContactDto> Contacts { get; set; } = new List<TenantContactDto>();
}