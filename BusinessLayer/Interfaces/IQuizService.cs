using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IQuizService
{
    Task<Result<Quiz>> GetQuizByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<Quiz>> CreateQuizAsync(Quiz quiz, CancellationToken ct = default);
    Task<Result> UpdateQuizAsync(Quiz quiz, CancellationToken ct = default);
    Task<Result> DeleteQuizAsync(Guid id, CancellationToken ct = default);
    Task<Result<object>> GetQuizStatisticsAsync(Guid quizId, CancellationToken ct = default);
    Task<Result<QuizAttempt>> SubmitQuizAsync(Guid quizId, Guid studentId, Dictionary<Guid, Guid> answers, int timeTakenSeconds = 0, CancellationToken ct = default);
    Task<Result<IEnumerable<QuizAttempt>>> GetStudentQuizAttemptsAsync(Guid quizId, Guid studentId, CancellationToken ct = default);
    Task<Result<IEnumerable<QuizAttempt>>> GetStudentQuizAttemptsAsync(Guid studentId, CancellationToken ct = default);
    Task<Result<Quiz>> GetQuizByLessonAsync(Guid lessonId, CancellationToken ct = default);
}
