using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CAASS.Auth.Models.Api.Request;
using CAASS.Auth.Models.Api.Response;
using CAASS.Auth.Models.Context;
using CAASS.Auth.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CAASS.Auth.Services.Implementations;

public class AuthRequestService : IAuthRequestService
{
    private readonly TenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<Tenant> _passwordHasher;
    
    public AuthRequestService(TenantContext tenantContext, IConfiguration configuration, IPasswordHasher<Tenant> passwordHasher)
    {
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
        _configuration = configuration;
    }

    public async Task<TenantAuthResponse> AuthenticateTenantAsync(TenantAuthRequest req)
    {
        if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.PasswordHash))
        {
            return new TenantAuthResponse("Missing one or more required fields.");
        }
        
        // Find tenant by email
        Tenant? tenant = await _tenantContext.Tenants
            .FirstOrDefaultAsync(t => t.Email == req.Email);
        
        if (tenant == null)
        {
            return new TenantAuthResponse("No Tenant found for entered Email.");
        }
        
        // Verify Tenant password
        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(tenant, tenant.Password, req.PasswordHash);

        if (passwordVerificationResult == PasswordVerificationResult.Failed && tenant.RetryCount <= 5)
        {
            tenant.RetryCount += 1;
            return new TenantAuthResponse("Supplied password was incorrect.");
        }

        if (tenant.RetryCount > 5)
        {
            return new TenantAuthResponse("Tenant account is locked.");
        }

        if (passwordVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // TODO: Rehash password
        }
        
        var (accessToken, expiresAt) = GenerateJwtToken(tenant);
        var refreshToken = GenerateRefreshToken();
        
        tenant.RefreshToken = refreshToken;
        tenant.RefreshTokenExpiryDateTime = expiresAt;
        await _tenantContext.SaveChangesAsync();

        return new TenantAuthResponse(new TenantAuthResponseData
        {
            TenantId = tenant.TenantId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        });
    }

    private (string accessToken, DateTime expiresAt) GenerateJwtToken(Tenant tenant)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration"));

        var expiryTime = DateTime.UtcNow.AddHours(1);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, tenant.TenantId.ToString()),
                new Claim(ClaimTypes.Role, "Tenant")
            ]),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = expiryTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"]
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiryTime);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}