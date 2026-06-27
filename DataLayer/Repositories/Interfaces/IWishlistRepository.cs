using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

public interface IWishlistRepository : IRepository<Wishlist>
{
    Task<IEnumerable<Wishlist>> GetStudentWishlistAsync(Guid studentId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
}
