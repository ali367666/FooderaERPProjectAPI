using Application.Common.Interfaces.Abstracts.İnterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/files")]
[ApiController]
[Authorize]
public class FilesController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/svg+xml"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly IFileStorageService _fileStorageService;

    public FilesController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, message = "Fayl seçilməyib." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { success = false, message = "Fayl 5MB-dan böyük ola bilməz." });

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { success = false, message = "Yalnız JPG, PNG, WEBP və ya SVG şəkil qəbul olunur." });

        await using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);

        return Ok(new { success = true, data = new { url } });
    }
}
