using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;

    public CategoryService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<IEnumerable<Category>>> GetAllCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _uow.Categories.Query().Include(c => c.Courses).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct);
        return Result<IEnumerable<Category>>.Success(categories);
    }

    public async Task<Result<Category>> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.Query().Include(c => c.Courses).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category == null) return Result<Category>.Failure("Category not found.");
        return Result<Category>.Success(category);
    }

    public async Task<Result<Category>> CreateCategoryAsync(Category category, CancellationToken ct = default)
    {
        Normalize(category);
        if (await _uow.Categories.ExistsAsync(c => c.Name == category.Name, ct)) return Result<Category>.Failure("Category name already exists.");
        if (await _uow.Categories.ExistsAsync(c => c.Slug == category.Slug, ct)) return Result<Category>.Failure("Category slug already exists.");
        await _uow.Categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Category>.Success(category);
    }

    public async Task<Result> UpdateCategoryAsync(Category category, CancellationToken ct = default)
    {
        Normalize(category);
        if (await _uow.Categories.ExistsAsync(c => c.Name == category.Name && c.Id != category.Id, ct)) return Result.Failure("Category name already exists.");
        if (await _uow.Categories.ExistsAsync(c => c.Slug == category.Slug && c.Id != category.Id, ct)) return Result.Failure("Category slug already exists.");
        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteCategoryAsync(int id, bool confirmWithCourses = false, CancellationToken ct = default)
    {
        var category = await _uow.Categories.Query().Include(c => c.Courses).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category == null) return Result.Failure("Category not found.");
        var activeCourseCount = category.Courses.Count(c => c.Status != CourseStatus.Archived);
        if (activeCourseCount > 0 && !confirmWithCourses) return Result.Failure($"This category contains {activeCourseCount} active course(s). Confirm the delete to deactivate it, or reassign those courses first.");
        if (category.Courses.Any()) { category.IsActive = false; _uow.Categories.Update(category); }
        else { _uow.Categories.Remove(category); }
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static void Normalize(Category category)
    {
        category.Name = category.Name.Trim();
        if (string.IsNullOrWhiteSpace(category.Slug)) category.Slug = Slugify(category.Name);
        category.Slug = Slugify(category.Slug);
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
    }
}

