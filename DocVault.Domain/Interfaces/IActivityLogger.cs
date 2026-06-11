namespace DocVault.Domain.Interfaces;

public interface IActivityLogger
{
    Task LogAsync(string action, string entityType, string? entityId, string? details, Guid? userId, string? ipAddress = null, CancellationToken ct = default);
}
