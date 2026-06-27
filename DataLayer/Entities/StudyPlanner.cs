namespace DataLayer.Entities;

public class StudyPlanner : BaseGuidEntity
{
    public Guid StudentId { get; set; }
    public DateTime ExamDate { get; set; }
    public decimal DailyStudyHours { get; set; } = 1.5m;
    public DateTime LastGeneratedAt { get; set; } = DateTime.UtcNow;

    public User Student { get; set; } = null!;
}
