using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Stay_BnB.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // Save uploaded property image to wwwroot/uploads/properties
    public async Task<string> SavePropertyImageAsync(IFormFile file)
    {
        return await SaveFileAsync(file, "properties");
    }

    // Save uploaded check-in verification document to wwwroot/uploads/documents
    public async Task<string> SaveDocumentAsync(IFormFile file)
    {
        return await SaveFileAsync(file, "documents");
    }

    private async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Uploaded file is empty");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", folder);

        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Generate unique filename with original extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{folder}/{uniqueFileName}";
    }

    public void DeleteFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var cleanPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(webRoot, cleanPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
