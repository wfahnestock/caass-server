namespace CAASS.Auth.Repositories;

public interface IRepository<T, TId> where TId : IEquatable<TId> where T : class
{
    Task<T?> GetByIdAsync(TId id);
    Task<T> CreateAsync(T obj);
    Task ReplaceAsync(T obj);
    Task UpsertAsync(T obj);
    Task DeleteAsync(T obj);
}