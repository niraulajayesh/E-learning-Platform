using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface ICertificateService
{
    Task<Result<Certificate>> GenerateCertificateAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<Result<Certificate>> GetCertificateByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<Certificate>> GetCertificateByNumberAsync(string certificateNumber, CancellationToken ct = default);
    Task<Result<Certificate>> GetCertificateByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<Result<IEnumerable<Certificate>>> GetStudentCertificatesAsync(Guid studentId, CancellationToken ct = default);
    Task<Result<IEnumerable<Certificate>>> GetIssuedCertificatesAsync(CancellationToken ct = default);
    Task<Result> RevokeCertificateAsync(Guid id, CancellationToken ct = default);
    Task<Result> ReissueCertificateAsync(Guid id, CancellationToken ct = default);
}
