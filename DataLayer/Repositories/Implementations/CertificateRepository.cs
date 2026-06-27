using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class CertificateRepository : Repository<Certificate>, ICertificateRepository
{
    public CertificateRepository(AppDbContext context) : base(context) { }

    public override async Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Student)
            .Include(c => c.Course)
                .ThenInclude(co => co.Instructor)
            .Include(c => c.Enrollment)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Certificate?> GetByNumberAsync(string certificateNumber, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Student)
            .Include(c => c.Course)
                .ThenInclude(co => co.Instructor)
            .Include(c => c.Enrollment)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, ct);

    public async Task<Certificate?> GetByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Student)
            .Include(c => c.Course)
                .ThenInclude(co => co.Instructor)
            .Include(c => c.Enrollment)
            .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId, ct);

    public async Task<IEnumerable<Certificate>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.StudentId == studentId)
            .Include(c => c.Student)
            .Include(c => c.Course)
                .ThenInclude(co => co.Instructor)
            .Include(c => c.Enrollment)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Certificate>> GetIssuedAsync(CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Student)
            .Include(c => c.Course)
                .ThenInclude(co => co.Instructor)
            .Include(c => c.Enrollment)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);

    public async Task<bool> ExistsByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
        => await _dbSet.AnyAsync(c => c.EnrollmentId == enrollmentId, ct);
}
