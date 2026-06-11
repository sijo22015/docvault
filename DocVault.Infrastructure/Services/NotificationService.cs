using DocVault.Domain.Entities;
using DocVault.Domain.Interfaces;
using DocVault.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public NotificationService(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task NotifyAsync(Guid userId, string title, string message, string type = "INFO", CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task NotifyAdminsAsync(string title, string message, string type = "INFO", CancellationToken ct = default)
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = admin.Id,
                Title = title,
                Message = message,
                Type = type
            });
        }
        await _db.SaveChangesAsync(ct);
    }
}
