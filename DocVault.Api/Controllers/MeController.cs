using System.Security.Claims;
using DocVault.Application.DTOs.Admin;
using DocVault.Application.DTOs.Common;
using DocVault.Domain.Entities;
using DocVault.Domain.Interfaces;
using DocVault.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly INotificationService _notifier;
    private readonly IFileStorage _storage;

    public MeController(UserManager<AppUser> userManager, AppDbContext db, INotificationService notifier, IFileStorage storage)
    {
        _userManager = userManager;
        _db = db;
        _notifier = notifier;
        _storage = storage;
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id)
            ? id
            : throw new UnauthorizedAccessException("Session expired. Please log in again.");

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile(CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        var photoUrl = !string.IsNullOrEmpty(user.ProfilePhotoPath)
            ? $"/api/v1/me/photo/{user.Id}"
            : null;
        return Ok(ApiResponse<UserDto>.Ok(
            new UserDto(user.Id, user.FullName, user.Email!, user.Department, user.UserStatus, user.CreatedAt, user.LastLoginAt, user.RevokedAt, roles, user.MobileNumber, user.WhatsAppNumber, photoUrl),
            HttpContext.TraceIdentifier));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user == null) return NotFound();
        user.FullName = request.FullName ?? user.FullName;
        user.MobileNumber = request.MobileNumber ?? user.MobileNumber;
        user.WhatsAppNumber = request.WhatsAppNumber ?? user.WhatsAppNumber;
        await _userManager.UpdateAsync(user);
        return Ok(ApiResponse<object>.Ok(new { message = "Profile updated." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("photo")]
    public async Task<ActionResult<ApiResponse<object>>> UploadPhoto(IFormFile photo, CancellationToken ct)
    {
        if (photo == null || photo.Length == 0)
            throw new InvalidOperationException("No file received.");

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(photo.ContentType.ToLower()))
            throw new InvalidOperationException("Please upload a JPEG, PNG, or WebP image.");

        if (photo.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Photo must be under 5 MB.");

        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user == null) return NotFound();

        // Delete old photo
        if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
            try { await _storage.PurgeAsync(user.ProfilePhotoPath, ct); } catch { /* ignore */ }

        var ext = photo.ContentType.ToLower() switch
        {
            "image/png"  => ".png",
            "image/webp" => ".webp",
            _            => ".jpg"
        };
        var fileName = $"{CurrentUserId}{ext}";

        using var stream = photo.OpenReadStream();
        var filePath = await _storage.SaveAsync(stream, fileName, "profile-photos", ct);

        user.ProfilePhotoPath = filePath;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse<object>.Ok(new { message = "Profile photo updated.", photoUrl = $"/api/v1/me/photo/{CurrentUserId}" }, HttpContext.TraceIdentifier));
    }

    [HttpGet("photo/{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPhoto(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || string.IsNullOrEmpty(user.ProfilePhotoPath) || !System.IO.File.Exists(user.ProfilePhotoPath))
            return NotFound();

        var contentType = Path.GetExtension(user.ProfilePhotoPath).ToLower() switch
        {
            ".png"  => "image/png",
            ".webp" => "image/webp",
            _       => "image/jpeg"
        };

        var stream = System.IO.File.OpenRead(user.ProfilePhotoPath);
        return File(stream, contentType);
    }
}

public record UpdateProfileRequest(string? FullName, string? MobileNumber, string? WhatsAppNumber);
