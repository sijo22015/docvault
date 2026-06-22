using DocVault.Application.DTOs.Auth;
using DocVault.Application.DTOs.Common;
using DocVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService auth, IWebHostEnvironment env)
    {
        _auth = auth;
        _env = env;
    }

    private void SetAuthCookies(string accessToken, string refreshToken, DateTime accessExpiry)
    {
        var isDev = _env.IsDevelopment();
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = !isDev,
            SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
            Expires  = new DateTimeOffset(accessExpiry),
            Path     = "/"
        });
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = !isDev,
            SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
            Expires  = DateTimeOffset.UtcNow.AddDays(7),
            Path     = "/api/v1/auth"
        });
    }

    private void ClearAuthCookies()
    {
        var isDev = _env.IsDevelopment();
        Response.Cookies.Delete("access_token",  new CookieOptions { Secure = !isDev, SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None, Path = "/" });
        Response.Cookies.Delete("refresh_token", new CookieOptions { Secure = !isDev, SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None, Path = "/api/v1/auth" });
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        await _auth.RegisterAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Registration submitted. Awaiting admin approval." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<UserSessionDto>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);
        return Ok(ApiResponse<UserSessionDto>.Ok(
            new UserSessionDto(result.UserId, result.Email, result.FullName, result.Role),
            HttpContext.TraceIdentifier));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<UserSessionDto>>> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = "No refresh token." });

        var result = await _auth.RefreshAsync(refreshToken, ct);
        SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);
        return Ok(ApiResponse<UserSessionDto>.Ok(
            new UserSessionDto(result.UserId, result.Email, result.FullName, result.Role),
            HttpContext.TraceIdentifier));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (!string.IsNullOrEmpty(refreshToken))
            try { await _auth.LogoutAsync(refreshToken, ct); } catch { /* best-effort revoke */ }
        ClearAuthCookies();
        return Ok(ApiResponse<object>.Ok(new { message = "Logged out." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "If that email exists, a reset link has been sent." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Password reset successfully." }, HttpContext.TraceIdentifier));
    }
}
