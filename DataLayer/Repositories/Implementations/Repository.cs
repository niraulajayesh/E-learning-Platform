using System.Linq.Expressions;
using DataLayer.Context;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

/// <summary>
/// Generic repository base providing standard EF Core CRUD operations.
/// All domain repositories inherit from this class.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // ── Queries ────────────────────────────────────────────────────────────

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public virtual async Task<T?> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    public virtual IQueryable<T> Query() => _dbSet.AsQueryable();

    // ── Commands ───────────────────────────────────────────────────────────

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(entities, ct);

    public virtual void Update(T entity)
        => _dbSet.Update(entity);

    public virtual void UpdateRange(IEnumerable<T> entities)
        => _dbSet.UpdateRange(entities);

    public virtual void Remove(T entity)
        => _dbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities)
        => _dbSet.RemoveRange(entities);
}
