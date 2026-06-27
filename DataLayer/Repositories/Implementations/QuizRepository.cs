using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class QuizRepository : Repository<Quiz>, IQuizRepository
{
    public QuizRepository(AppDbContext context) : base(context) { }

    public async Task<Quiz?> GetWithQuestionsAsync(Guid quizId, CancellationToken ct = default)
        => await _dbSet
            .Include(q => q.Questions.OrderBy(q => q.Order))
                .ThenInclude(q => q.Answers.OrderBy(a => a.Order))
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);

    public async Task<Quiz?> GetByLessonAsync(Guid lessonId, CancellationToken ct = default)
        => await _dbSet
            .Include(q => q.Questions.OrderBy(q => q.Order))
                .ThenInclude(q => q.Answers.OrderBy(a => a.Order))
            .FirstOrDefaultAsync(q => q.LessonId == lessonId, ct);

    public async Task<IEnumerable<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid studentId, CancellationToken ct = default)
        => await _context.QuizAttempts
            .Where(a => a.QuizId == quizId && a.StudentId == studentId)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync(ct);

    public async Task<QuizAttempt?> GetLastAttemptAsync(Guid quizId, Guid studentId, CancellationToken ct = default)
        => await _context.QuizAttempts
            .Where(a => a.QuizId == quizId && a.StudentId == studentId)
            .OrderByDescending(a => a.AttemptedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<int> GetAttemptCountAsync(Guid quizId, Guid studentId, CancellationToken ct = default)
        => await _context.QuizAttempts
            .CountAsync(a => a.QuizId == quizId && a.StudentId == studentId, ct);

    public async Task AddAttemptAsync(QuizAttempt attempt, CancellationToken ct = default)
        => await _context.QuizAttempts.AddAsync(attempt, ct);
}
