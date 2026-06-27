using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _uow;

    public WishlistService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IEnumerable<Wishlist>>> GetStudentWishlistAsync(Guid studentId, CancellationToken ct = default)
    {
        var items = await _uow.Wishlists.GetStudentWishlistAsync(studentId, ct);
        return Result<IEnumerable<Wishlist>>.Success(items);
    }

    public async Task<Result> AddToWishlistAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
    {
        if (await _uow.Wishlists.ExistsAsync(studentId, courseId, ct))
            return Result.Failure("Course is already in your wishlist.");

        var item = new Wishlist { StudentId = studentId, CourseId = courseId };
        await _uow.Wishlists.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveFromWishlistAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
    {
        var item = await _uow.Wishlists.GetFirstOrDefaultAsync(w => w.StudentId == studentId && w.CourseId == courseId, ct);
        if (item == null) return Result.Failure("Item not found in wishlist.");

        _uow.Wishlists.Remove(item);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<bool>> IsInWishlistAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
    {
        var exists = await _uow.Wishlists.ExistsAsync(studentId, courseId, ct);
        return Result<bool>.Success(exists);
    }
}
