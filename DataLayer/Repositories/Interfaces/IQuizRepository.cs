using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Quiz repository with question/answer eager loading and attempt history.
/// </summary>
public interface IQuizRepository : IRepository<Quiz>
{
    Task<Quiz?> GetWithQuestionsAsync(Guid quizId, CancellationToken ct = default);
    Task<Quiz?> GetByLessonAsync(Guid lessonId, CancellationToken ct = default);
    Task<IEnumerable<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid studentId, CancellationToken ct = default);
    Task<QuizAttempt?> GetLastAttemptAsync(Guid quizId, Guid studentId, CancellationToken ct = default);
    Task<int> GetAttemptCountAsync(Guid quizId, Guid studentId, CancellationToken ct = default);
    Task AddAttemptAsync(QuizAttempt attempt, CancellationToken ct = default);
}
