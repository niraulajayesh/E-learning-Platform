using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class WishlistRepository : Repository<Wishlist>, IWishlistRepository
{
    public WishlistRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Wishlist>> GetStudentWishlistAsync(Guid studentId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(w => w.Course)
                .ThenInclude(c => c.Instructor)
            .Where(w => w.StudentId == studentId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(w => w.StudentId == studentId && w.CourseId == courseId, ct);
    }
}
