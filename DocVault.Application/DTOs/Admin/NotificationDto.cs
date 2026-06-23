namespace DocVault.Application.DTOs.Admin;

public record NotificationDto(
    int Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt
);
