using CAASS.Auth.Models.Entities;

namespace CAASS.Auth.Repositories;

public interface ITenantRepository : IRepository<Tenant, int>
{
    Task<Tenant?> GetByEmailAsync(string email);
    Task<IEnumerable<Tenant>> GetManyByIdAsync(IEnumerable<int> ids);
    Task<IEnumerable<Tenant>> GetManyByEmailAsync(IEnumerable<string> emails);
}