namespace DataLayer.Entities;

public class Wishlist : BaseGuidEntity
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }

    // Navigation properties
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
