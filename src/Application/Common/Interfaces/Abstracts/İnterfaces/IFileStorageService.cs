namespace Application.Common.Interfaces.Abstracts.İnterfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken);
}
