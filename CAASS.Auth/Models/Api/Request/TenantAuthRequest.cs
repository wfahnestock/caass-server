using System.ComponentModel.DataAnnotations;
using CAASS.Auth.Enums.Auth;

namespace CAASS.Auth.Models.Api.Request;

public record TenantAuthRequest
{
    [Required]
    public required string Email { get; set; }
    [Required]
    public required string PasswordHash { get; set; }
    /// <summary>
    /// AuthType indicates where the request is coming from,
    /// for example, from a mobile app or the web app.
    /// </summary>
    [Required]
    public AuthType AuthType { get; set; }
}