using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;

namespace DataLayer.Repositories.Implementations;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context) { }

    public async Task<Payment?> GetByGatewayReferenceAsync(string reference, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(p => p.GatewayReference == reference, ct);

    public async Task<IEnumerable<Payment>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
        => await _dbSet
            .Where(p => p.StudentId == studentId)
            .Include(p => p.Course)
            .Include(p => p.Coupon)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Payment>> GetByCourseAsync(Guid courseId, CancellationToken ct = default)
        => await _dbSet
            .Where(p => p.CourseId == courseId)
            .Include(p => p.Student)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
        => await _dbSet
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, ct);

    public async Task<decimal> GetInstructorRevenueAsync(Guid instructorId, CancellationToken ct = default)
        => await _dbSet
            .Where(p => p.Status == PaymentStatus.Completed &&
                        p.Course.InstructorId == instructorId)
            .SumAsync(p => p.Amount, ct);
}
