using CAASS.Auth.Models.Entities;
using CAASS.Auth.Utils;

namespace CAASS.Auth.Services.Implementations;

public class TenantConnectionBuilder(IConfiguration configuration) : ITenantConnectionBuilder
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public string BuildTenantConnectionString(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (string.IsNullOrWhiteSpace(tenant.TenantSlug)) throw new ArgumentException("Tenant slug cannot be null or empty.", nameof(tenant.TenantSlug));
        
        // Build the host dynamically based on the tenant slug
        string host = $"{tenant.TenantSlug}-db";
        
        string dbName = $"{tenant.OrganizationName}_db";
        string dbUser = "caass";
        string dbPassword = CryptoDeterministicStringGenerator.Generate(tenant.TenantSlug, 24);

        string connectionString = $"Host={host};Port=5432;Database={dbName};Username={dbUser};Password={dbPassword}";
        
        return connectionString;
    }
}