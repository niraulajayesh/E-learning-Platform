namespace DataLayer.Entities;

/// <summary>
/// Represents a course category. Supports self-referencing hierarchy for sub-categories.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? BannerUrl { get; set; }
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navigation properties
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
