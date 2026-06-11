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

    public AuthService(UserManager<AppUser> userManager, AppDbContext db, IConfiguration config, IActivityLogger logger, INotificationService notifier)
    {
        _userManager = userManager;
        _db = db;
        _config = config;
        _logger = logger;
        _notifier = notifier;
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
        var role = roles.Contains("Admin") ? "Admin" : "User";

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
        var role = roles.Contains("Admin") ? "Admin" : "User";
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
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return;
        await _logger.LogAsync("FORGOT_PASSWORD", "User", user.Id.ToString(), null, user.Id, ct: ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");
        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        await _logger.LogAsync("RESET_PASSWORD", "User", user.Id.ToString(), null, user.Id, ct: ct);
    }

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
