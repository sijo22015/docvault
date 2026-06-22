namespace DocVault.Application.DTOs.Auth;

public record UserSessionDto(
    string UserId,
    string Email,
    string FullName,
    string Role
);
