namespace DataLayer.Entities;

public class Banner : BaseEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ButtonText { get; set; }
    public string? ButtonLink { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
