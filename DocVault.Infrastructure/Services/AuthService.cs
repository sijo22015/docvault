using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DocVault.Application.DTOs.Auth;
using DocVault.Application.Services;
using DocVault.Domain.Entities;
using DocVault.Domain.Interfaces;
using DocVault.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DocVault.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifier;
    private readonly IEmailSender _email;
    private readonly IMemoryCache _cache;

    private record OtpEntry(string CodeHash, int Attempts);

    public AuthService(UserManager<AppUser> userManager, AppDbContext db, IConfiguration config,
        IActivityLogger logger, INotificationService notifier, IEmailSender email, IMemoryCache cache)
    {
        _userManager = userManager;
        _db = db;
        _config = config;
        _logger = logger;
        _notifier = notifier;
        _email = email;
        _cache = cache;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Department = request.Department,
            UserStatus = "PENDING"
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "User");
        await _logger.LogAsync("REGISTER", "User", user.Id.ToString(), $"Registration for {user.Email}", user.Id, ct: ct);
        await _notifier.NotifyAdminsAsync("New Registration", $"{user.FullName} ({user.Email}) has registered and is pending approval.", ct: ct);

        return new AuthResponse(string.Empty, string.Empty, DateTime.UtcNow, user.Id.ToString(), user.Email!, user.FullName, "User");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.UserStatus != "APPROVED")
            throw new UnauthorizedAccessException($"Account status is {user.UserStatus}. Login is not permitted.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.Contains("Admin") ? "Admin" : roles.Contains("SecondaryAdmin") ? "SecondaryAdmin" : "User";

        var (accessToken, expiry) = GenerateAccessToken(user, role);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ct);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _logger.LogAsync("LOGIN", "User", user.Id.ToString(), null, user.Id, ct: ct);

        return new AuthResponse(accessToken, refreshToken, expiry, user.Id.ToString(), user.Email!, user.FullName, role);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        if (stored.User.UserStatus != "APPROVED")
            throw new UnauthorizedAccessException("Account is not approved.");

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;

        var roles = await _userManager.GetRolesAsync(stored.User);
        var role = roles.Contains("Admin") ? "Admin" : roles.Contains("SecondaryAdmin") ? "SecondaryAdmin" : "User";
        var (accessToken, expiry) = GenerateAccessToken(stored.User, role);
        var newRefresh = await CreateRefreshTokenAsync(stored.User.Id, ct);
        stored.ReplacedByToken = HashToken(newRefresh);

        await _db.SaveChangesAsync(ct);
        return new AuthResponse(accessToken, newRefresh, expiry, stored.User.Id.ToString(), stored.User.Email!, stored.User.FullName, role);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        if (stored != null)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        // Always return success — never reveal whether an email exists (security)
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || user.UserStatus != "APPROVED") return;

        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        var cacheKey = $"otp:{request.Email.ToLowerInvariant()}";
        _cache.Set(cacheKey, new OtpEntry(HashCode(code), 0), TimeSpan.FromMinutes(15));

        var html = BuildOtpEmail(code, user.FullName);
        await _email.SendAsync(request.Email, "DocVault — Your Password Reset Code", html, ct);
        await _logger.LogAsync("FORGOT_PASSWORD", "User", user.Id.ToString(), null, user.Id, ct: ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var cacheKey = $"otp:{request.Email.ToLowerInvariant()}";

        if (!_cache.TryGetValue(cacheKey, out OtpEntry? entry) || entry == null)
            throw new InvalidOperationException("Verification code has expired. Please request a new one.");

        if (entry.Attempts >= 3)
        {
            _cache.Remove(cacheKey);
            throw new InvalidOperationException("Too many incorrect attempts. Please request a new code.");
        }

        if (entry.CodeHash != HashCode(request.Code))
        {
            _cache.Set(cacheKey, entry with { Attempts = entry.Attempts + 1 }, TimeSpan.FromMinutes(15));
            var left = 3 - (entry.Attempts + 1);
            throw new InvalidOperationException($"Invalid verification code. {left} attempt{(left == 1 ? "" : "s")} remaining.");
        }

        _cache.Remove(cacheKey);

        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        await _logger.LogAsync("RESET_PASSWORD", "User", user.Id.ToString(), null, user.Id, ct: ct);
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    private static string BuildOtpEmail(string code, string name) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"></head>
        <body style="margin:0;padding:20px;background:#f8fafc;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
          <div style="max-width:480px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
            <div style="background:linear-gradient(135deg,#4c1d95 0%,#6d28d9 50%,#1e40af 100%);padding:32px;text-align:center;">
              <div style="font-size:28px;font-weight:800;color:#fff;letter-spacing:-0.5px;">DocVault</div>
              <div style="color:rgba(255,255,255,0.75);font-size:14px;margin-top:6px;">Password Reset Verification</div>
            </div>
            <div style="padding:32px;">
              <p style="color:#374151;font-size:15px;margin:0 0 8px;">Hi {name},</p>
              <p style="color:#6b7280;font-size:14px;line-height:1.6;margin:0 0 24px;">
                We received a request to reset your DocVault password. Enter the code below to continue.
              </p>
              <div style="background:#f5f3ff;border:2px dashed #c4b5fd;border-radius:12px;padding:28px;text-align:center;">
                <div style="font-size:44px;font-weight:800;letter-spacing:14px;color:#4c1d95;font-family:monospace;">{code}</div>
                <div style="color:#7c3aed;font-size:13px;margin-top:10px;font-weight:500;">⏱ Valid for 15 minutes · 3 attempts allowed</div>
              </div>
              <div style="background:#fef3c7;border-left:4px solid #f59e0b;border-radius:8px;padding:12px 16px;margin-top:24px;font-size:13px;color:#92400e;">
                🛡️ If you didn't request this, ignore this email — your password will remain unchanged.
              </div>
            </div>
            <div style="background:#f8fafc;border-top:1px solid #e2e8f0;padding:16px 32px;text-align:center;color:#94a3b8;font-size:12px;">
              © 2026 DocVault · Automated message — do not reply
            </div>
          </div>
        </body>
        </html>
        """;

    private (string Token, DateTime Expiry) GenerateAccessToken(AppUser user, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:AccessTokenMinutes"] ?? "15"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("role", role),
            new Claim("fullName", user.FullName),
            new Claim("department", user.Department)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken ct)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var days = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7");
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        });
        await _db.SaveChangesAsync(ct);
        return raw;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
