using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using CAASS.Auth.Messaging;
using CAASS.Auth.Models.Api.Request;
using CAASS.Auth.Models.Api.Response;
using CAASS.Auth.Models.Context;
using CAASS.Auth.Models.Entities;
using CAASS.Auth.Models.Entities.Events;
using CAASS.Auth.Utils;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tenant = CAASS.Auth.Models.Entities.Tenant;

namespace CAASS.Auth.Services.Implementations;

public class AuthRequestService(
    TenantContext tenantContext,
    IConfiguration configuration,
    IPasswordHasher<Tenant> passwordHasher,
    IMapper tenantContactMapper,
    IRabbitMqPublisher<TenantCreatedEvent> tenantCreatedEventPublisher)
    : IAuthRequestService
{
    public async Task<TenantAuthResponse> AuthenticateTenantAsync(TenantAuthRequest req)
    {
        if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
        {
            return new TenantAuthResponse("Missing one or more required fields.");
        }
        
        // Find tenant by email
        Tenant? tenant = await tenantContext.Tenants
            .FirstOrDefaultAsync(t => t.Email == req.Email);
        
        if (tenant == null)
        {
            return new TenantAuthResponse("No Tenant found for entered Email.");
        }
        
        // Verify Tenant password
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(tenant, tenant.Password, req.Password);

        if (passwordVerificationResult == PasswordVerificationResult.Failed && tenant.RetryCount <= 5)
        {
            tenant.RetryCount += 1;
            return new TenantAuthResponse("Supplied password was incorrect.");
        }

        if (tenant.IsLocked || tenant.RetryCount > 5)
        {
            return new TenantAuthResponse("Tenant account is locked.");
        }

        if (passwordVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            tenant.Password = passwordHasher.HashPassword(tenant, req.Password);
            await tenantContext.SaveChangesAsync();
        }
        
        var (accessToken, expiresAt) = GenerateJwtToken(tenant);
        var refreshToken = GenerateRefreshToken();
        
        if (tenant.RetryCount >= 1) tenant.RetryCount = 0; // Reset retry count on successful login
        tenant.RefreshToken = refreshToken;
        tenant.RefreshTokenExpiryDateTime = expiresAt;
        tenant.LastLogin = DateTime.UtcNow;
        await tenantContext.SaveChangesAsync();

        return new TenantAuthResponse(new TenantAuthResponseData
        {
            TenantId = tenant.TenantId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        });
    }

    public async Task<TenantCreateResponse> CreateTenantAsync(TenantCreateRequest req)
    {
        // Check if tenant already exists first
        Tenant? existingTenant = await tenantContext.Tenants
            .FirstOrDefaultAsync(t => t.Email == req.Email);

        if (existingTenant != null)
        {
            return new TenantCreateResponse("A Tenant account already exists for the provided email.");
        }
        
        // Create a new Tenant
        Tenant tenant = new()
        {
            TenantId = Guid.CreateVersion7(),
            Email = req.Email,
            Password = string.Empty,
            CreatedAt = DateTime.UtcNow,
            // Tenant can't log in until verifying their email, set to null for now.
            LastLogin = null,
            RetryCount = 0,
            IsActive = true,
            IsLocked = false,
            LockedReason = null,
            OrganizationName = req.OrganizationName,
            OrganizationEmailDomain = req.Email.Split('@').Last(),
            TenantSlug = $"{req.OrganizationName.ToLower().RemoveWhitespace()}",
            RefreshToken = null,
            RefreshTokenExpiryDateTime = null
        };

        var tenantContacts = tenantContactMapper.Map<List<TenantContact>>(req.Contacts, 
            opt => opt.Items["TenantId"] = tenant.TenantId);
        
        tenant.Password = passwordHasher.HashPassword(tenant, req.Password);
        
        // Save the new Tenant and their contacts to the database
        await tenantContext.Tenants.AddAsync(tenant);
        await tenantContext.TenantContacts.AddRangeAsync(tenantContacts);
        await tenantContext.SaveChangesAsync();

        // Publish the TenantCreated event to the message bus - this will trigger the provisioning of the tenant's database
        // and any other necessary setup.
        var tenantCreatedEvent = new TenantCreatedEvent
        {
            TenantId = tenant.TenantId,
            TenantSlug = tenant.TenantSlug,
        };
        
        await tenantCreatedEventPublisher.PublishAsync(tenantCreatedEvent, RabbitMqSettings.RabbitMqQueues.TenantCreatedQueue);
        
        return new TenantCreateResponse(new TenantCreateResponseData
        {
            TenantId = tenant.TenantId
        });
    }

    private (string accessToken, DateTime expiresAt) GenerateJwtToken(Tenant tenant)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration"));

        var expiryTime = DateTime.UtcNow.AddHours(1);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("TenantId", tenant.TenantId.ToString()),
                new Claim(ClaimTypes.Role, "Tenant")
            ]),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = expiryTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = configuration["JWT:Issuer"],
            Audience = configuration["JWT:Audience"]
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