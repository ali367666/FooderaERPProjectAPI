using Application.BackOffice;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>What the till page needs beside the movements themselves.</summary>
[ApiController]
[Authorize]
[Route("api/CashMovements")]
public class CashDeskController : ControllerBase
{
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["application/pdf"] = ".pdf"
    };

    private const long MaxFileBytes = 5 * 1024 * 1024;

    private readonly ICashDeskService _cashDesk;
    private readonly IFileStorageService _files;
    private readonly ICurrentUserService _currentUser;

    public CashDeskController(ICashDeskService cashDesk, IFileStorageService files, ICurrentUserService currentUser)
    {
        _cashDesk = cashDesk;
        _files = files;
        _currentUser = currentUser;
    }

    [Authorize(Policy = AppPermissions.CashRegisterView)]
    [HttpGet("summary")]
    public async Task<ActionResult<CashDeskSummary>> Summary([FromQuery] int restaurantId, CancellationToken ct)
        => Ok(await _cashDesk.GetSummaryAsync(_currentUser.CompanyId, restaurantId, ct));

    [Authorize(Policy = AppPermissions.CashRegisterView)]
    [HttpGet("categories")]
    public async Task<ActionResult<List<CashExpenseCategoryResponse>>> Categories(CancellationToken ct)
        => Ok(await _cashDesk.GetCategoriesAsync(_currentUser.CompanyId, ct));

    [Authorize(Policy = AppPermissions.CashRegisterManage)]
    [HttpPost("categories")]
    public async Task<ActionResult<CashExpenseCategoryResponse>> AddCategory(
        [FromBody] SaveCashExpenseCategoryRequest request, CancellationToken ct)
        => Ok(await _cashDesk.AddCategoryAsync(_currentUser.CompanyId, request.Name, ct));

    [Authorize(Policy = AppPermissions.CashRegisterManage)]
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        await _cashDesk.DeleteCategoryAsync(_currentUser.CompanyId, id, ct);
        return NoContent();
    }

    /// <summary>Stores the receipt / document of a movement and returns its address, to be sent as attachmentUrl.</summary>
    [Authorize(Policy = AppPermissions.CashRegisterManage)]
    [HttpPost("attachment")]
    public async Task<IActionResult> Attachment(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, message = "Fayl seçilməyib." });
        if (file.Length > MaxFileBytes)
            return BadRequest(new { success = false, message = "Fayl 5MB-dan böyük ola bilməz." });
        if (!Extensions.TryGetValue(file.ContentType, out var extension))
            return BadRequest(new { success = false, message = "Yalnız JPG, PNG, WEBP və ya PDF qəbul olunur." });

        await using var stream = file.OpenReadStream();
        var url = await _files.UploadAsync(stream, "cash" + extension, file.ContentType, ct);
        return Ok(new { success = true, data = new { url } });
    }
}
