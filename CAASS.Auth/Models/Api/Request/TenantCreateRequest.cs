using System.ComponentModel.DataAnnotations;
using CAASS.Auth.Models.Api.Dto;

namespace CAASS.Auth.Models.Api.Request;

public record TenantCreateRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    public required string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Organization name is required")]
    public string OrganizationName { get; set; } = string.Empty;
    
    public required string StreetAddress { get; set; } = string.Empty;
    public required string City { get; set; } = string.Empty;
    public required string State { get; set; } = string.Empty;
    public required string ZipCode { get; set; } = string.Empty;
    public string? CountryCode { get; set; } = "US";
    
    public IEnumerable<TenantContactDto> Contacts { get; set; } = new List<TenantContactDto>();
}