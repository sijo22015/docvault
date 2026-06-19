namespace DocVault.Application.DTOs.Admin;

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Department,
    string Status,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    DateTime? RevokedAt,
    IList<string> Roles,
    string? MobileNumber = null,
    string? WhatsAppNumber = null,
    string? CommunicationAddress = null,
    string? ProfilePhotoUrl = null
);
