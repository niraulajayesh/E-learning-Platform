namespace DataLayer.Entities;

public class Testimonial : BaseEntity
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentRole { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
}
