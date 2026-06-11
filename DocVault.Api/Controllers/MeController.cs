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

    public MeController(UserManager<AppUser> userManager, AppDbContext db, INotificationService notifier)
    {
        _userManager = userManager;
        _db = db;
        _notifier = notifier;
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
        return Ok(ApiResponse<UserDto>.Ok(
            new UserDto(user.Id, user.FullName, user.Email!, user.Department, user.UserStatus, user.CreatedAt, user.LastLoginAt, user.RevokedAt, roles),
            HttpContext.TraceIdentifier));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user == null) return NotFound();
        user.FullName = request.FullName ?? user.FullName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        await _userManager.UpdateAsync(user);
        return Ok(ApiResponse<object>.Ok(new { message = "Profile updated." }, HttpContext.TraceIdentifier));
    }
}

public record UpdateProfileRequest(string? FullName, string? PhoneNumber);
