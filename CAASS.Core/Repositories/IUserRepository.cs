using CAASS.Core.Models.Schema;

namespace CAASS.Core.Repositories;

public interface IUserRepository : IRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetManyByIdAsync(IEnumerable<int> ids);
    Task<IEnumerable<User>> GetManyByEmailAsync(IEnumerable<string> emails);
}