using DocVault.Domain.Entities;
using DocVault.Domain.Interfaces;
using DocVault.Infrastructure.Data;

namespace DocVault.Infrastructure.Services;

public class ActivityLogger : IActivityLogger
{
    private readonly AppDbContext _db;

    public ActivityLogger(AppDbContext db) => _db = db;

    public async Task LogAsync(string action, string entityType, string? entityId, string? details, Guid? userId, string? ipAddress = null, CancellationToken ct = default)
    {
        var log = new ActivityLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            UserId = userId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow.Add(TimeSpan.FromHours(5.5)) // store as IST
        };
        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
