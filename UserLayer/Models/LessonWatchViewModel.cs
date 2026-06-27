using DataLayer.Entities;

namespace UserLayer.Models;

public class LessonWatchViewModel
{
    public Enrollment Enrollment { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Lesson CurrentLesson { get; set; } = null!;
    public List<Lesson> Lessons { get; set; } = new();
    public IReadOnlySet<Guid> CompletedLessonIds { get; set; } = new HashSet<Guid>();
    public Lesson? PreviousLesson { get; set; }
    public Lesson? NextLesson { get; set; }

    public bool IsCurrentLessonCompleted => CompletedLessonIds.Contains(CurrentLesson.Id);
}
