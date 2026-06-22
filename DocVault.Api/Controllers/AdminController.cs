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
[Authorize(Roles = "Admin")]
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
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _admin.GetUsersAsync(status, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveUser(Guid id, CancellationToken ct)
    {
        await _admin.ApproveUserAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "User approved." }, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeUser(Guid id, CancellationToken ct)
    {
        await _admin.RevokeUserAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "User revoked." }, HttpContext.TraceIdentifier));
    }

    [HttpGet("dashboard/summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(CancellationToken ct)
    {
        var result = await _admin.GetDashboardSummaryAsync(ct);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("dashboard/analytics")]
    public async Task<ActionResult<ApiResponse<AnalyticsDto>>> GetAnalytics([FromQuery] string fy, CancellationToken ct)
    {
        var result = await _admin.GetAnalyticsAsync(fy, ct);
        return Ok(ApiResponse<AnalyticsDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("documents")]
    public async Task<ActionResult<ApiResponse<PagedResult<DocumentDto>>>> SearchDocuments(
        [FromQuery] DocumentSearchRequest request, CancellationToken ct)
    {
        var result = await _admin.SearchDocumentsAsync(request, ct);
        return Ok(ApiResponse<PagedResult<DocumentDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/{id:guid}/hard")]
    public async Task<ActionResult<ApiResponse<object>>> AdminHardDeleteDocument(Guid id, CancellationToken ct)
    {
        await _admin.AdminHardDeleteDocumentAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document permanently deleted." }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> AdminSoftDeleteDocument(Guid id, CancellationToken ct)
    {
        await _admin.AdminSoftDeleteDocumentAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document deleted by admin." }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/deleted")]
    public async Task<ActionResult<ApiResponse<object>>> PurgeUserDeleted(CancellationToken ct)
    {
        var count = await _admin.AdminPurgeDeletedDocumentsAsync(CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Permanently deleted {count} document(s).", count }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("documents/deleted-by-admin")]
    public async Task<ActionResult<ApiResponse<object>>> PurgeAdminDeleted(CancellationToken ct)
    {
        var count = await _admin.AdminPurgeAdminDeletedDocumentsAsync(CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Permanently deleted {count} document(s).", count }, HttpContext.TraceIdentifier));
    }

    [HttpPost("documents/{id:guid}/restore")]
    public async Task<ActionResult<ApiResponse<object>>> AdminRestoreDocument(Guid id, CancellationToken ct)
    {
        await _admin.AdminRestoreDocumentAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document restored by admin." }, HttpContext.TraceIdentifier));
    }

    [HttpGet("documents/{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken ct)
    {
        var doc = await _docs.GetByIdAsync(id, null, isAdmin: true, ct);
        if (doc == null) return NotFound();
        var stream = await _docs.DownloadAsync(id, null, isAdmin: true, ct);
        return File(stream, doc.ContentType, doc.OriginalFileName);
    }

    [HttpGet("documents/verify-integrity")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> VerifyIntegrity([FromQuery] int fyId, CancellationToken ct)
    {
        var results = await _docs.VerifyIntegrityAsync(fyId, ct);
        var mapped = results.Select(r => new { r.FileName, r.Hash, r.Mismatch });
        return Ok(ApiResponse<IEnumerable<object>>.Ok(mapped, HttpContext.TraceIdentifier));
    }

    [HttpPost("financial-years/{id:int}/lock")]
    public async Task<ActionResult<ApiResponse<object>>> LockFY(int id, CancellationToken ct)
    {
        await _admin.LockFinancialYearAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Financial year locked." }, HttpContext.TraceIdentifier));
    }

    [HttpGet("export/fy/{fyId:int}")]
    public async Task<IActionResult> ExportFY(int fyId, CancellationToken ct)
    {
        var zip = await _admin.ExportFinancialYearZipAsync(fyId, ct);
        return File(zip, "application/zip", $"docvault-export-fy{fyId}.zip");
    }

    [HttpPost("documents/merge-pdf")]
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

    [HttpDelete("activity-logs")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteActivityLogs(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var query = _db.ActivityLogs.AsQueryable();
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value.Date);
        if (to.HasValue)   query = query.Where(l => l.CreatedAt < to.Value.Date.AddDays(1));

        int count = await query.ExecuteDeleteAsync(ct);
        var detail = (from.HasValue || to.HasValue)
            ? $"Deleted {count} log(s) in range {from:d} – {to:d}"
            : $"Deleted all {count} activity log(s)";
        await _logger.LogAsync("DELETE_ACTIVITY_LOGS", "ActivityLog", null, detail, CurrentUserId, ct: ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Deleted {count} activity log(s).", count }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("activity-logs/selected")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSelectedActivityLogs(
        [FromBody] long[] ids,
        CancellationToken ct)
    {
        int count = await _db.ActivityLogs.Where(l => ids.Contains(l.Id)).ExecuteDeleteAsync(ct);
        await _logger.LogAsync("DELETE_ACTIVITY_LOGS", "ActivityLog", null,
            $"Deleted {count} selected activity log(s)", CurrentUserId, ct: ct);
        return Ok(ApiResponse<object>.Ok(new { message = $"Deleted {count} activity log(s).", count }, HttpContext.TraceIdentifier));
    }
}
