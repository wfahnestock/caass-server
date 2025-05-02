using System.ComponentModel.DataAnnotations;

namespace CAASS.Auth.Models.Api.Dto;

public class TenantContactDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "First Name is required")]
    public required string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Last Name is required")]
    public required string LastName { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; } = string.Empty;
    
    public string? Title { get; set; } = string.Empty;
    
    public TenantContactType ContactType { get; set; }
    
    public required string StreetAddress { get; set; } = string.Empty;
    public required string City { get; set; } = string.Empty;
    public required string State { get; set; } = string.Empty;
    public required string ZipCode { get; set; } = string.Empty;
}

public enum TenantContactType
{
    Primary = 1,
    Billing = 2,
    Technical = 3
}