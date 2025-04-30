namespace CAASS.Auth.Models.Entities;

/// <summary>
/// TenantContact entity holds information about each Tenant's contact.
/// A Tenant can have multiple contacts. A TenantContact should be a person
/// with authorization to make decisions on behalf of a Tenant. For example,
/// billing, support, or technical contacts. A Tenant will always have at least
/// one TenantContact, as this entity is created when a Tenant is created.
/// <param name="TenantId">The ID of the Tenant this contact belongs to.</param>
/// <param name="Tenant">Required mapping to principal entity for EF Core. You shouldn't reference this in code.</param>
/// <param name="FirstName">The first name of this TenantContact.</param>
/// <param name="LastName">The last name of this TenantContact.</param>
/// <param name="Email">The email address of this TenantContact.</param>
/// <param name="PhoneNumber">The phone number of this TenantContact.</param>
/// <param name="Title">The title held by this TenantContact within their district.</param>
/// <param name="IsPrimaryContact">Is this TenantContact the primary contact for this Tenant?</param>
/// <param name="IsBillingContact">Is this TenantContact the primary billing contact for this Tenant?</param>
/// <param name="IsTechnicalContact">Is this TenantContact the primary technical contact for this Tenant?</param>
/// <param name="CreatedAt">The DateTime this TenantContact was created.</param>
/// <param name="UpdatedAt">The DateTime this TenantContact was last updated.</param>
/// <param name="Address">The address for this TenantContact. Usually will be their district office.</param>
/// <param name="City">The city for this TenantContact.</param>
/// <param name="State">The state for this TenantContact.</param>
/// <param name="ZipCode">The zip code for this TenantContact.</param>
/// <param name="Country">The ISO-3166 country code for this TenantContact</param>
/// </summary>
public record TenantContact
{
    public required Guid TenantContactId { get; set; }
    public required Guid TenantId { get; set; }
    public Tenant Tenant { get; init; } = null!; // Required reference navigation to principal entity (Tenant)
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Title { get; set; }
    public required bool IsPrimaryContact { get; set; }
    public required bool IsBillingContact { get; set; }
    public required bool IsTechnicalContact { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string ZipCode { get; set; }
    public string? CountryCode { get; set; } = "US";
}