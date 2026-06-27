namespace DataLayer.Entities;

public class StudyGuide : BaseGuidEntity
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Theory { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;
    public string KeyConcepts { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public int DisplayOrder { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<StudyGuideBookmark> Bookmarks { get; set; } = new List<StudyGuideBookmark>();
}
