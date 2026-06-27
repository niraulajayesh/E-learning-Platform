using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;

namespace DataLayer.Repositories.Implementations;

public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(AppDbContext context) : base(context) { }

    public async Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e =>
            e.StudentId == studentId && e.CourseId == courseId, ct);

    public async Task<Enrollment?> GetWithProgressAsync(Guid enrollmentId, CancellationToken ct = default)
        => await _dbSet
            .Include(e => e.ProgressRecords)
                .ThenInclude(p => p.Lesson)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

    public async Task<IEnumerable<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
        => await _dbSet
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
            .Include(e => e.Course)
                .ThenInclude(c => c.Category)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Enrollment>> GetByCourseAsync(Guid courseId, CancellationToken ct = default)
        => await _dbSet
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);

    public async Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
        => await _dbSet.AnyAsync(e =>
            e.StudentId == studentId &&
            e.CourseId == courseId &&
            (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed), ct);

    public async Task<int> GetEnrollmentCountAsync(Guid courseId, CancellationToken ct = default)
        => await _dbSet.CountAsync(e => e.CourseId == courseId, ct);
}
