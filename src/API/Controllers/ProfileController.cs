using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Common.Responce;
using Application.MyProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>The signed-in user's own profile. No permission is needed: the user id always comes from the token.</summary>
[Route("api/profile")]
[ApiController]
[Authorize]
public class ProfileController : ControllerBase
{
    private static readonly Dictionary<string, string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

    private readonly IProfileService _profileService;
    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly IFileStorageService _fileStorageService;

    public ProfileController(
        IProfileService profileService,
        IAuthenticatedUserAccessor userAccessor,
        IFileStorageService fileStorageService)
    {
        _profileService = profileService;
        _userAccessor = userAccessor;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = await _userAccessor.ResolveUserIdAsync(cancellationToken);
        var profile = await _profileService.GetAsync(userId, cancellationToken);
        if (profile is null)
            return NotFound(BaseResponse<ProfileDto>.Fail("İstifadəçi tapılmadı."));
        return Ok(BaseResponse<ProfileDto>.Ok(profile));
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(BaseResponse.Fail("Fayl seçilməyib."));

        if (file.Length > MaxPhotoSizeBytes)
            return BadRequest(BaseResponse.Fail("Şəkil 5MB-dan böyük ola bilməz."));

        if (!PhotoExtensions.TryGetValue(file.ContentType, out var extension))
            return BadRequest(BaseResponse.Fail("Yalnız JPG, PNG və ya WEBP şəkil qəbul olunur."));

        var userId = await _userAccessor.ResolveUserIdAsync(cancellationToken);

        await using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadAsync(stream, "avatar" + extension, file.ContentType, cancellationToken);

        if (!await _profileService.SetAvatarAsync(userId, url, cancellationToken))
            return NotFound(BaseResponse.Fail("İstifadəçi tapılmadı."));

        return Ok(BaseResponse<string>.Ok(url, "Şəkil yükləndi."));
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> RemoveAvatar(CancellationToken cancellationToken)
    {
        var userId = await _userAccessor.ResolveUserIdAsync(cancellationToken);
        if (!await _profileService.SetAvatarAsync(userId, null, cancellationToken))
            return NotFound(BaseResponse.Fail("İstifadəçi tapılmadı."));
        return Ok(BaseResponse.Ok("Şəkil silindi."));
    }
}
