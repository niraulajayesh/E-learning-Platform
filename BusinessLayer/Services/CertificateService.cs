using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class CertificateService : ICertificateService
{
    private readonly IUnitOfWork _uow;

    public CertificateService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<Certificate>> GenerateCertificateAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var existingCertificate = await _uow.Certificates.GetByEnrollmentAsync(enrollmentId, ct);
        if (existingCertificate != null) return Result<Certificate>.Success(existingCertificate);
        var enrollment = await _uow.Enrollments.GetByIdAsync(enrollmentId, ct);
        if (enrollment == null) return Result<Certificate>.Failure("Enrollment not found.");
        if (enrollment.Status != EnrollmentStatus.Completed || enrollment.CompletionPercentage < 100) return Result<Certificate>.Failure("Course not fully completed yet.");

        var requiredQuizIds = await _uow.Quizzes.Query().Where(q => q.Lesson.Section.CourseId == enrollment.CourseId && q.Lesson.IsPublished).Select(q => q.Id).ToListAsync(ct);
        if (requiredQuizIds.Any())
        {
            var passedQuizIds = await _uow.Quizzes.Query().SelectMany(q => q.Attempts).Where(a => a.StudentId == enrollment.StudentId && a.IsPassed && requiredQuizIds.Contains(a.QuizId)).Select(a => a.QuizId).Distinct().ToListAsync(ct);
            if (passedQuizIds.Count < requiredQuizIds.Count) return Result<Certificate>.Failure("Pass all required quizzes before earning a certificate.");
        }

        var certificateNumber = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var certificate = new Certificate { EnrollmentId = enrollmentId, StudentId = enrollment.StudentId, CourseId = enrollment.CourseId, CertificateNumber = certificateNumber, IssuedAt = DateTime.UtcNow };
        certificate.VerificationUrl = $"/Certificates/Verify/{certificate.Id}";
        await _uow.Certificates.AddAsync(certificate, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Certificate>.Success(certificate);
    }

    public async Task<Result<Certificate>> GetCertificateByIdAsync(Guid id, CancellationToken ct = default)
    {
        var certificate = await _uow.Certificates.GetByIdAsync(id, ct);
        if (certificate == null) return Result<Certificate>.Failure("Certificate not found.");
        return Result<Certificate>.Success(certificate);
    }

    public async Task<Result<Certificate>> GetCertificateByNumberAsync(string certificateNumber, CancellationToken ct = default)
    {
        var certificate = await _uow.Certificates.GetByNumberAsync(certificateNumber, ct);
        if (certificate == null) return Result<Certificate>.Failure("Certificate not found.");
        return Result<Certificate>.Success(certificate);
    }

    public async Task<Result<Certificate>> GetCertificateByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var certificate = await _uow.Certificates.GetByEnrollmentAsync(enrollmentId, ct);
        if (certificate == null) return Result<Certificate>.Failure("Certificate not found.");
        return Result<Certificate>.Success(certificate);
    }

    public async Task<Result<IEnumerable<Certificate>>> GetStudentCertificatesAsync(Guid studentId, CancellationToken ct = default)
    {
        var certificates = await _uow.Certificates.GetByStudentAsync(studentId, ct);
        return Result<IEnumerable<Certificate>>.Success(certificates);
    }

    public async Task<Result<IEnumerable<Certificate>>> GetIssuedCertificatesAsync(CancellationToken ct = default)
    {
        var certificates = await _uow.Certificates.GetIssuedAsync(ct);
        return Result<IEnumerable<Certificate>>.Success(certificates);
    }

    public async Task<Result> RevokeCertificateAsync(Guid id, CancellationToken ct = default)
    {
        var certificate = await _uow.Certificates.GetByIdAsync(id, ct);
        if (certificate == null) return Result.Failure("Certificate not found.");
        certificate.VerificationUrl = null;
        certificate.PdfUrl = null;
        _uow.Certificates.Update(certificate);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReissueCertificateAsync(Guid id, CancellationToken ct = default)
    {
        var certificate = await _uow.Certificates.GetByIdAsync(id, ct);
        if (certificate == null) return Result.Failure("Certificate not found.");
        certificate.CertificateNumber = Guid.NewGuid().ToString("N").ToUpperInvariant();
        certificate.VerificationUrl = $"/Certificates/Verify/{certificate.Id}";
        certificate.IssuedAt = DateTime.UtcNow;
        _uow.Certificates.Update(certificate);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

