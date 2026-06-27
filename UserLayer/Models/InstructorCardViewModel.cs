using DataLayer.Entities;

namespace UserLayer.Models;

public class InstructorCardViewModel
{
    public User Instructor { get; set; } = null!;
    public int PublishedCourseCount { get; set; }
    public int TotalStudents { get; set; }
    public double AverageRating { get; set; }
}
