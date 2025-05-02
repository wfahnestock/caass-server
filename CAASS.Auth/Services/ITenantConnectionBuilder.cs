using CAASS.Auth.Models.Entities;

namespace CAASS.Auth.Services;

public interface ITenantConnectionBuilder
{
    string BuildTenantConnectionString(Tenant tenant);
}