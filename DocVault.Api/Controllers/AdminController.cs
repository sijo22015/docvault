using System.Security.Claims;
using DocVault.Application.DTOs.Admin;
using DocVault.Application.DTOs.Common;
using DocVault.Application.DTOs.Documents;
using DocVault.Application.Services;
using DocVault.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IDocumentService _docs;
    private readonly AppDbContext _db;
    public AdminController(IAdminService admin, IDocumentService docs, AppDbContext db) { _admin = admin; _docs = docs; _db = db; }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id)
            ? id
            : throw new UnauthorizedAccessException("Session expired. Please log in again.");

    [HttpGet("users")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var excludeAdmins = User.IsInRole("SecondaryAdmin");
        var result = await _admin.GetUsersAsync(status, page, pageSize, excludeAdmins, ct);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveUser(Guid id, CancellationToken ct)
    {
        await _admin.ApproveUserAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "User approved." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/revoke")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeUser(Guid id, CancellationToken ct)
    {
        await _admin.RevokeUserAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "User revoked." }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(Guid id, CancellationToken ct)
    {
        await _admin.DeleteUserAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "User permanently deleted." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/make-secondary-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> MakeSecondaryAdmin(Guid id, CancellationToken ct)
    {
        await _admin.MakeSecondaryAdminAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Secondary Admin role granted." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/revoke-secondary-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeSecondaryAdmin(Guid id, CancellationToken ct)
    {
        await _admin.RevokeSecondaryAdminAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Secondary Admin role revoked." }, HttpContext.TraceIdentifier));
    }

    [HttpGet("dashboard/summary")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(CancellationToken ct)
    {
        var result = await _admin.GetDashboardSummaryAsync(ct);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("dashboard/analytics")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<ActionResult<ApiResponse<AnalyticsDto>>> GetAnalytics([FromQuery] string fy, CancellationToken ct)
    {
        var result = await _admin.GetAnalyticsAsync(fy, ct);
        return Ok(ApiResponse<AnalyticsDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("documents")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<ActionResult<ApiResponse<PagedResult<DocumentDto>>>> SearchDocuments(
        [FromQuery] DocumentSearchRequest request, CancellationToken ct)
    {
        var result = await _admin.SearchDocumentsAsync(request, ct);
        return Ok(ApiResponse<PagedResult<DocumentDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPatch("documents/{id:guid}")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> AdminUpdateDocument(Guid id, [FromBody] UpdateDocumentRequest request, CancellationToken ct)
    {
        var result = await _docs.UpdateDocumentAsync(id, CurrentUserId, isAdmin: true, request, ct);
        return Ok(ApiResponse<DocumentDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/{id:guid}/hard")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> AdminHardDeleteDocument(Guid id, CancellationToken ct)
    {
        await _admin.AdminHardDeleteDocumentAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document permanently deleted." }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> AdminSoftDeleteDocument(Guid id, CancellationToken ct)
    {
        await _admin.AdminSoftDeleteDocumentAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document deleted by admin." }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> PurgeUserDeleted(CancellationToken ct)
    {
        var count = await _admin.AdminPurgeDeletedDocumentsAsync(CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Permanently deleted {count} document(s).", count }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/deleted-by-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> PurgeAdminDeleted(CancellationToken ct)
    {
        var count = await _admin.AdminPurgeAdminDeletedDocumentsAsync(CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Permanently deleted {count} document(s).", count }, HttpContext.TraceIdentifier));
    }

    [HttpPost("documents/{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> AdminRestoreDocument(Guid id, CancellationToken ct)
    {
        await _admin.AdminRestoreDocumentAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document restored by admin." }, HttpContext.TraceIdentifier));
    }

    [HttpGet("documents/{id:guid}/download")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken ct)
    {
        var doc = await _docs.GetByIdAsync(id, null, isAdmin: true, ct);
        if (doc == null) return NotFound();
        var stream = await _docs.DownloadAsync(id, null, isAdmin: true, ct);
        return File(stream, doc.ContentType, doc.OriginalFileName);
    }

    [HttpGet("documents/verify-integrity")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> VerifyIntegrity([FromQuery] int fyId, CancellationToken ct)
    {
        var results = await _docs.VerifyIntegrityAsync(fyId, ct);
        var mapped = results.Select(r => new { r.FileName, r.Hash, r.Mismatch });
        return Ok(ApiResponse<IEnumerable<object>>.Ok(mapped, HttpContext.TraceIdentifier));
    }

    [HttpPost("financial-years/{id:int}/lock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> LockFY(int id, CancellationToken ct)
    {
        await _admin.LockFinancialYearAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Financial year locked." }, HttpContext.TraceIdentifier));
    }

    [HttpGet("export/fy/{fyId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportFY(int fyId, CancellationToken ct)
    {
        var zip = await _admin.ExportFinancialYearZipAsync(fyId, ct);
        return File(zip, "application/zip", $"docvault-export-fy{fyId}.zip");
    }

    [HttpPost("documents/merge-pdf")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MergeDocumentsPdf([FromBody] MergePdfRequest? request, CancellationToken ct)
    {
        try
        {
            var (data, fileName) = await _admin.MergeDocumentsToPdfAsync(request ?? new MergePdfRequest(), ct);
            return File(data, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(400, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    [HttpGet("activity-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<object>>>> GetActivityLogs(
        [FromQuery] string? action,
        [FromQuery] string? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _db.ActivityLogs.AsQueryable();
        if (!string.IsNullOrEmpty(action)) query = query.Where(l => l.Action == action.ToUpperInvariant());
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var uid)) query = query.Where(l => l.UserId == uid);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value.Date);
        if (to.HasValue)   query = query.Where(l => l.CreatedAt < to.Value.Date.AddDays(1));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(l => l.User)
            .Select(l => (object)new
            {
                l.Id,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.Details,
                l.IpAddress,
                l.CreatedAt,
                UserName = l.User != null ? l.User.FullName : null
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<PagedResult<object>>.Ok(new PagedResult<object>(items, total, page, pageSize), HttpContext.TraceIdentifier));
    }

    [HttpPost("activity-logs/delete")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteActivityLogs(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var count = await _admin.DeleteActivityLogsAsync(from, to, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Deleted {count} activity log(s).", count }, HttpContext.TraceIdentifier));
    }

    [HttpPost("activity-logs/delete-selected")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSelectedActivityLogs(
        [FromBody] long[] ids,
        CancellationToken ct)
    {
        var count = await _admin.DeleteSelectedActivityLogsAsync(ids, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Deleted {count} activity log(s).", count }, HttpContext.TraceIdentifier));
    }

    [HttpGet("notifications")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications(CancellationToken ct)
    {
        var items = await _admin.GetNotificationsAsync(CurrentUserId, ct);
        return Ok(ApiResponse<List<NotificationDto>>.Ok(items, HttpContext.TraceIdentifier));
    }

    [HttpPost("notifications/mark-read")]
    [Authorize(Roles = "Admin,SecondaryAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> MarkNotificationsRead(CancellationToken ct)
    {
        await _admin.MarkNotificationsReadAsync(CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Marked as read." }, HttpContext.TraceIdentifier));
    }
}
