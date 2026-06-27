using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IWishlistService
{
    Task<Result<IEnumerable<Wishlist>>> GetStudentWishlistAsync(Guid studentId, CancellationToken ct = default);
    Task<Result> AddToWishlistAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<Result> RemoveFromWishlistAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<Result<bool>> IsInWishlistAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
}
