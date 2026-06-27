using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IFileService
{
    Task<Result<string>> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<Result> DeleteFileAsync(string fileUrl, CancellationToken ct = default);
}
