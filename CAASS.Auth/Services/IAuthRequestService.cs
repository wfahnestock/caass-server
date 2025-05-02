using CAASS.Auth.Models.Api.Request;
using CAASS.Auth.Models.Api.Response;

namespace CAASS.Auth.Services;

public interface IAuthRequestService
{
    Task<TenantAuthResponse> AuthenticateTenantAsync(TenantAuthRequest req);
    Task<TenantCreateResponse> CreateTenantAsync(TenantCreateRequest req);
}