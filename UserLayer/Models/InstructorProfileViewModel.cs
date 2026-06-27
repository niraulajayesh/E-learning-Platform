using DataLayer.Entities;

namespace UserLayer.Models;

public class InstructorProfileViewModel
{
    public User Instructor { get; set; } = null!;
    public List<Course> Courses { get; set; } = new();
    public int TotalStudents { get; set; }
    public double AverageRating { get; set; }
}
