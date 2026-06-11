namespace DocVault.Domain.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string title, string message, string type = "INFO", CancellationToken ct = default);
    Task NotifyAdminsAsync(string title, string message, string type = "INFO", CancellationToken ct = default);
}
