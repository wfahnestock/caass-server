using CAASS.Auth.Models.Api.Request;
using CAASS.Auth.Models.Api.Response;
using CAASS.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAASS.Auth.Controllers;

/**
 * AuthRequestController handles authorization and authentication for Tenants.
 * These are methods like Login, Register, Logout, etc.
 */
[ApiController]
[Route("[controller]")]
public class AuthRequestController(IAuthRequestService authService, ILogger<AuthRequestController> logger)
    : ControllerBase
{
    [HttpPost("authenticate")]
    [AllowAnonymous]
    public async Task<ActionResult<TenantAuthResponse>> Authenticate([FromBody] TenantAuthRequest req)
    {
        logger.LogInformation("Authentication Request: {TenantAuthRequest}", req);

        var response = await authService.AuthenticateTenantAsync(req);

        if (response.IsError)
        {
            logger.LogInformation("Authentication failed: {Error}", response.Message);
            return Unauthorized(response);
        }
        
        logger.LogInformation("Authentication successful: {TenantId}", response.Data?.TenantId);
        return Ok(response);
    }

    [HttpPost("create")]
    [AllowAnonymous]
    public async Task<ActionResult<TenantCreateResponse>> CreateTenant([FromBody] TenantCreateRequest req)
    {
        logger.LogInformation("Create Request: {TenantCreateRequest}", req);
        
        var response = await authService.CreateTenantAsync(req);

        if (response.IsError)
        {
            logger.LogInformation("Authentication failed: {Error}", response.Message);
            return Unauthorized(response);
        }
        
        logger.LogInformation("Authentication successful: {TenantId}", response.Data?.TenantId);
        return Ok(response);
    }
}