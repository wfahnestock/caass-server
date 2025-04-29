using System.ComponentModel.DataAnnotations;
using CAASS.Core.Enums.Auth;

namespace CAASS.Core.Models.Api.Request;

public record UserAuthRequest
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string PasswordHash { get; set; }
    /// <summary>
    /// AuthType indicates where the request is coming from,
    /// for example, from a mobile app or the web app.
    /// </summary>
    [Required]
    public AuthType AuthType { get; set; }
}