using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class LessonRepository : Repository<Lesson>, ILessonRepository
{
    public LessonRepository(AppDbContext context) : base(context) { }

    public async Task<Lesson?> GetWithQuizAsync(Guid lessonId, CancellationToken ct = default)
        => await _dbSet.Include(l => l.Quiz).ThenInclude(q => q!.Questions.OrderBy(q => q.Order)).ThenInclude(q => q.Answers.OrderBy(a => a.Order)).FirstOrDefaultAsync(l => l.Id == lessonId, ct);

    public async Task<IEnumerable<Lesson>> GetBySectionAsync(Guid sectionId, CancellationToken ct = default)
        => await _dbSet.Where(l => l.SectionId == sectionId).OrderBy(l => l.Order).ToListAsync(ct);

    public async Task<IEnumerable<Lesson>> GetByCourseAsync(Guid courseId, CancellationToken ct = default)
        => await _dbSet.Include(l => l.Section).Where(l => l.Section.CourseId == courseId).OrderBy(l => l.Section.Order).ThenBy(l => l.Order).ToListAsync(ct);

    public async Task<int> GetMaxOrderInSectionAsync(Guid sectionId, CancellationToken ct = default)
    {
        var max = await _dbSet.Where(l => l.SectionId == sectionId).MaxAsync(l => (int?)l.Order, ct);
        return max ?? 0;
    }
}
