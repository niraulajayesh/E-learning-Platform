using System.Linq.Expressions;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Generic repository interface providing standard CRUD and query operations.
/// All domain-specific repositories extend this interface.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IRepository<T> where T : class
{
    // ── Queries ────────────────────────────────────────────────────────────

    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    IQueryable<T> Query();   // Allows callers to compose further LINQ before materialising

    // ── Commands ───────────────────────────────────────────────────────────

    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}
