using Microsoft.AspNetCore.Http;

namespace Stay_BnB.Services;

public interface IFileService
{
    Task<string> SavePropertyImageAsync(IFormFile file);
    Task<string> SaveDocumentAsync(IFormFile file);
    void DeleteFile(string relativePath);
}
