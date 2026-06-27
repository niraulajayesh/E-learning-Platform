using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface ICategoryService
{
    Task<Result<IEnumerable<Category>>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<Result<Category>> GetCategoryByIdAsync(int id, CancellationToken ct = default);
    Task<Result<Category>> CreateCategoryAsync(Category category, CancellationToken ct = default);
    Task<Result> UpdateCategoryAsync(Category category, CancellationToken ct = default);
    Task<Result> DeleteCategoryAsync(int id, bool confirmWithCourses = false, CancellationToken ct = default);
}
