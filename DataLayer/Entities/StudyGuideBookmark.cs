namespace DataLayer.Entities;

public class StudyGuideBookmark : BaseGuidEntity
{
    public Guid StudyGuideId { get; set; }
    public Guid StudentId { get; set; }

    public StudyGuide StudyGuide { get; set; } = null!;
    public User Student { get; set; } = null!;
}
