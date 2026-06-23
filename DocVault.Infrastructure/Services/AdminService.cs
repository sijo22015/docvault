using Dapper;
using DocVault.Application.DTOs.Admin;
using DocVault.Application.DTOs.Common;
using DocVault.Application.DTOs.Documents;
using DocVault.Application.Services;
using DocVault.Domain.Entities;
using DocVault.Domain.Interfaces;
using DocVault.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;

namespace DocVault.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifier;
    private readonly IFileStorage _storage;
    private readonly string _connStr;

    public AdminService(AppDbContext db, UserManager<AppUser> userManager, IActivityLogger logger, INotificationService notifier, IFileStorage storage, IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _notifier = notifier;
        _storage = storage;
        _connStr = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(u => u.UserStatus == status.ToUpperInvariant());

        var total = await query.CountAsync(ct);
        var users = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = new List<UserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            dtos.Add(new UserDto(u.Id, u.FullName, u.Email!, u.Department, u.UserStatus, u.CreatedAt, u.LastLoginAt, u.RevokedAt, roles));
        }
        return new PagedResult<UserDto>(dtos, total, page, pageSize);
    }

    public async Task ApproveUserAsync(Guid userId, Guid adminId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        user.UserStatus = "APPROVED";
        await _userManager.UpdateAsync(user);
        await _logger.LogAsync("APPROVE_USER", "User", userId.ToString(), null, adminId, ct: ct);
        await _notifier.NotifyAsync(userId, "Account Approved", "Your account has been approved. You can now log in.", ct: ct);
    }

    public async Task RevokeUserAsync(Guid userId, Guid adminId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        user.UserStatus = "REVOKED";
        user.RevokedAt = DateTime.UtcNow;
        user.RevokedBy = adminId.ToString();
        await _userManager.UpdateAsync(user);
        var tokens = _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked);
        await tokens.ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true).SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        await _logger.LogAsync("REVOKE_USER", "User", userId.ToString(), null, adminId, ct: ct);
        await _notifier.NotifyAsync(userId, "Account Revoked", "Your account access has been revoked.", ct: ct);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        using var conn = new NpgsqlConnection(_connStr);
        var totalUsers = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"AspNetUsers\"");
        var pending = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"AspNetUsers\" WHERE \"UserStatus\" = 'PENDING'");
        var totalDocs = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM documents WHERE is_deleted = false");
        var thisMonth = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM documents WHERE is_deleted = false AND uploaded_at >= date_trunc('month', NOW())");

        var byDept = (await conn.QueryAsync<(string dept, int cnt)>(
            "SELECT d.name AS dept, COUNT(doc.id) AS cnt FROM departments d LEFT JOIN documents doc ON doc.department_id = d.id AND doc.is_deleted = false GROUP BY d.name ORDER BY cnt DESC LIMIT 10"))
            .Select(x => new DeptDocCount(x.dept, x.cnt)).ToList();

        var recent = (await conn.QueryAsync<(string action, string userName, string entityType, DateTime createdAt)>(
            @"SELECT al.action, u.""FullName"", al.entity_type, al.created_at
              FROM activity_logs al LEFT JOIN ""AspNetUsers"" u ON u.""Id"" = al.user_id
              ORDER BY al.created_at DESC LIMIT 10"))
            .Select(x => new RecentActivity(x.action, x.userName, x.entityType, x.createdAt)).ToList();

        return new DashboardSummaryDto(totalUsers, pending, totalDocs, thisMonth, byDept, recent);
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(string financialYear, CancellationToken ct = default)
    {
        using var conn = new NpgsqlConnection(_connStr);
        var fyId = await conn.ExecuteScalarAsync<int>("SELECT id FROM financial_years WHERE label = @label", new { label = financialYear });

        var deptProgress = (await conn.QueryAsync<(string dept, int required, int submitted)>(
            @"SELECT d.name,
                     COALESCE(req.cnt, 0) as required,
                     COALESCE(sub.cnt, 0) as submitted
              FROM departments d
              LEFT JOIN (SELECT department_id, COUNT(*) as cnt FROM required_documents WHERE financial_year_id = @fyId AND is_active = true GROUP BY department_id) req ON req.department_id = d.id
              LEFT JOIN (SELECT department_id, COUNT(*) as cnt FROM documents WHERE financial_year_id = @fyId AND is_deleted = false AND status = 'SUBMITTED' GROUP BY department_id) sub ON sub.department_id = d.id
              WHERE d.is_active = true", new { fyId }))
            .Select(x => new DeptProgress(x.dept, x.required, x.submitted, x.required > 0 ? Math.Round((double)x.submitted / x.required * 100, 1) : 0))
            .ToList();

        var monthly = (await conn.QueryAsync<(string month, int cnt)>(
            @"SELECT TO_CHAR(uploaded_at, 'YYYY-MM') as month, COUNT(*) as cnt
              FROM documents WHERE financial_year_id = @fyId AND is_deleted = false
              GROUP BY month ORDER BY month", new { fyId }))
            .Select(x => new MonthlyTrend(x.month, x.cnt)).ToList();

        var topContrib = (await conn.QueryAsync<(string userName, string dept, int cnt)>(
            @"SELECT u.""FullName"", u.""Department"", COUNT(doc.id) as cnt
              FROM documents doc JOIN ""AspNetUsers"" u ON u.""Id"" = doc.user_id
              WHERE doc.financial_year_id = @fyId AND doc.is_deleted = false
              GROUP BY u.""Id"", u.""FullName"", u.""Department""
              ORDER BY cnt DESC LIMIT 10", new { fyId }))
            .Select(x => new TopContributor(x.userName, x.dept, x.cnt)).ToList();

        return new AnalyticsDto(financialYear, deptProgress, monthly, topContrib);
    }

    public async Task<PagedResult<DocumentDto>> SearchDocumentsAsync(DocumentSearchRequest request, CancellationToken ct = default)
    {
        var query = _db.Documents.AsQueryable();
        if (request.OnlyDeleted)
            query = query.Where(d => d.IsDeleted);
        else if (!request.IncludeDeleted)
            query = query.Where(d => !d.IsDeleted);
        if (!string.IsNullOrEmpty(request.UserId)) query = query.Where(d => d.UserId.ToString() == request.UserId);
        if (request.DepartmentId.HasValue) query = query.Where(d => d.DepartmentId == request.DepartmentId);
        if (request.FinancialYearId.HasValue) query = query.Where(d => d.FinancialYearId == request.FinancialYearId);
        if (request.DocumentTypeId.HasValue) query = query.Where(d => d.DocumentTypeId == request.DocumentTypeId);
        if (!string.IsNullOrEmpty(request.Status)) query = query.Where(d => d.Status == request.Status);
        if (request.FromDate.HasValue) query = query.Where(d => d.UploadedAt >= request.FromDate);
        if (request.ToDate.HasValue) query = query.Where(d => d.UploadedAt <= request.ToDate);
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term) ||
                                     (d.Description != null && d.Description.ToLower().Contains(term)));
        }
        if (!string.IsNullOrEmpty(request.UploaderName))
        {
            var uploaderTerm = request.UploaderName.ToLower();
            query = query.Where(d => d.User != null && d.User.FullName.ToLower().Contains(uploaderTerm));
        }
        if (request.OnlyDeletedByAdmin.HasValue)
            query = query.Where(d => d.DeletedByAdmin == request.OnlyDeletedByAdmin.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(d => d.UploadedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Include(d => d.User).Include(d => d.Department).Include(d => d.FinancialYear).Include(d => d.DocumentType)
            .ToListAsync(ct);

        var dtos = items.Select(d => new DocumentDto(d.Id, d.Title, d.Description, d.OriginalFileName, d.ContentType, d.FileSizeBytes, d.Status, d.Tags, d.UploadedAt, d.SubmittedAt, d.IsDeleted, d.DeletedAt, d.User?.FullName ?? "", d.Department?.Name ?? "", d.FinancialYear?.Label ?? "", d.DocumentType?.Name ?? "", d.DeletedByAdmin));
        return new PagedResult<DocumentDto>(dtos, total, request.Page, request.PageSize);
    }

    public async Task LockFinancialYearAsync(int fyId, Guid adminId, CancellationToken ct = default)
    {
        var fy = await _db.FinancialYears.FindAsync([fyId], ct)
            ?? throw new InvalidOperationException("Financial year not found.");
        fy.IsLocked = true;
        fy.LockedAt = DateTime.UtcNow;
        fy.LockedBy = adminId.ToString();
        await _db.SaveChangesAsync(ct);
        await _logger.LogAsync("LOCK_FY", "FinancialYear", fyId.ToString(), fy.Label, adminId, ct: ct);
    }

    public async Task<(byte[] Data, string FileName)> MergeDocumentsToPdfAsync(MergePdfRequest request, CancellationToken ct = default)
    {
        var query = _db.Documents.AsQueryable().Where(d => !d.IsDeleted);

        if (request.DocumentIds is { Length: > 0 })
        {
            var guids = request.DocumentIds.Select(Guid.Parse).ToArray();
            query = query.Where(d => guids.Contains(d.Id));
        }
        else
        {
            if (request.DepartmentId.HasValue)   query = query.Where(d => d.DepartmentId   == request.DepartmentId);
            if (request.FinancialYearId.HasValue) query = query.Where(d => d.FinancialYearId == request.FinancialYearId);
            if (request.DocumentTypeId.HasValue) query = query.Where(d => d.DocumentTypeId  == request.DocumentTypeId);
            if (!string.IsNullOrEmpty(request.Status))     query = query.Where(d => d.Status == request.Status);
            if (!string.IsNullOrEmpty(request.SearchTerm))
                query = query.Where(d => d.Title.Contains(request.SearchTerm) || (d.Description != null && d.Description.Contains(request.SearchTerm)));
            if (!string.IsNullOrEmpty(request.UploaderName))
                query = query.Where(d => d.User != null && d.User.FullName.Contains(request.UploaderName));
        }

        var docs = await query
            .Include(d => d.Department).Include(d => d.FinancialYear)
            .Include(d => d.DocumentType).Include(d => d.User)
            .OrderBy(d => d.FinancialYear!.FyStart).ThenBy(d => d.UploadedAt)
            .ToListAsync(ct);

        if (docs.Count == 0)
            throw new InvalidOperationException("No documents match the current filters.");

        var output = new PdfDocument();
        output.Info.Title  = string.IsNullOrEmpty(request.SearchTerm) ? "Merged Documents" : request.SearchTerm;
        output.Info.Author = "DocVault";

        PdfMergeHelper.AddCoverPage(output, docs);

        foreach (var doc in docs)
        {
            PdfMergeHelper.AddSeparatorPage(output, doc);
            if (Path.GetExtension(doc.OriginalFileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var stream = await _storage.ReadAsync(doc.FilePath, ct);
                    using var input  = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                    for (int i = 0; i < input.PageCount; i++)
                        output.AddPage(input.Pages[i]);
                }
                catch { /* separator page already marks the entry; skip corrupt file */ }
            }
        }

        using var ms = new MemoryStream();
        output.Save(ms);

        var safeName = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? "merged-documents"
            : string.Concat(request.SearchTerm.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c)).Trim('-');

        return (ms.ToArray(), $"{safeName}-merged.pdf");
    }

    public async Task<byte[]> ExportFinancialYearZipAsync(int fyId, CancellationToken ct = default)
    {
        var docs = await _db.Documents
            .Where(d => d.FinancialYearId == fyId && !d.IsDeleted)
            .Include(d => d.User).Include(d => d.Department).Include(d => d.FinancialYear).Include(d => d.DocumentType)
            .ToListAsync(ct);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var manifest = new System.Text.StringBuilder("OriginalFileName,Sha256Hash,Uploader,Department,DocumentType,UploadedAt\n");
            foreach (var doc in docs)
            {
                try
                {
                    using var fs = await _storage.ReadAsync(doc.FilePath, ct);
                    var entry = zip.CreateEntry(Path.Combine(doc.Department?.Name ?? "unknown", doc.StoredFileName));
                    using var entryStream = entry.Open();
                    await fs.CopyToAsync(entryStream, ct);
                    manifest.AppendLine($"{doc.OriginalFileName},{doc.Sha256Hash},{doc.User?.FullName},{doc.Department?.Name},{doc.DocumentType?.Name},{doc.UploadedAt:O}");
                }
                catch { /* skip missing files */ }
            }
            var manifestEntry = zip.CreateEntry("manifest.csv");
            using var mw = new StreamWriter(manifestEntry.Open());
            await mw.WriteAsync(manifest.ToString());
        }
        return ms.ToArray();
    }

    public async Task AdminRestoreDocumentAsync(Guid documentId, Guid adminId, CancellationToken ct = default)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Document not found or not deleted.");
        doc.IsDeleted = false;
        // DeletedAt intentionally kept — signals "admin-restored, not visible to user"
        await _db.SaveChangesAsync(ct);
        await _storage.RestoreAsync(doc.FilePath, ct);
        await _logger.LogAsync("RESTORE", "Document", doc.Id.ToString(), doc.Title, adminId, ct: ct);
    }

    public async Task AdminSoftDeleteDocumentAsync(Guid documentId, Guid adminId, CancellationToken ct = default)
    {
        var doc = await _db.Documents.FindAsync([documentId], ct)
            ?? throw new InvalidOperationException("Document not found.");
        if (doc.IsDeleted)
            throw new InvalidOperationException("Document is already deleted.");
        doc.IsDeleted = true;
        doc.DeletedAt = DateTime.UtcNow;
        doc.DeletedByAdmin = true;
        await _db.SaveChangesAsync(ct);
        await _logger.LogAsync("ADMIN_DELETE", "Document", doc.Id.ToString(), doc.Title, adminId, ct: ct);
    }

    public async Task AdminHardDeleteDocumentAsync(Guid documentId, Guid adminId, CancellationToken ct = default)
    {
        var doc = await _db.Documents
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Document not found or not in a deleted state.");

        try { await _storage.PurgeAsync(doc.FilePath, ct); } catch { /* ignore missing files */ }
        _db.DocumentVersions.RemoveRange(doc.Versions);
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync(ct);
        await _logger.LogAsync("HARD_DELETE", "Document", doc.Id.ToString(), doc.Title, adminId, ct: ct);
    }

    public async Task<int> AdminPurgeDeletedDocumentsAsync(Guid adminId, CancellationToken ct = default)
    {
        var docs = await _db.Documents
            .Include(d => d.Versions)
            .Where(d => d.IsDeleted && !d.DeletedByAdmin)   // user-deleted only
            .ToListAsync(ct);

        foreach (var doc in docs)
            try { await _storage.PurgeAsync(doc.FilePath, ct); } catch { /* ignore missing files */ }

        _db.DocumentVersions.RemoveRange(docs.SelectMany(d => d.Versions));
        _db.Documents.RemoveRange(docs);
        await _db.SaveChangesAsync(ct);
        await _logger.LogAsync("PURGE_DELETED", "Document", null, $"Permanently deleted {docs.Count} user-deleted document(s)", adminId, ct: ct);
        return docs.Count;
    }

    public async Task<int> DeleteActivityLogsAsync(DateTime? from, DateTime? to, Guid adminId, CancellationToken ct = default)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            await _db.Database.OpenConnectionAsync(ct);
            try
            {
                using var cmd = conn.CreateCommand();
                if (!from.HasValue && !to.HasValue)
                {
                    cmd.CommandText = "DELETE FROM activity_logs";
                }
                else
                {
                    var conditions = new List<string>();
                    if (from.HasValue)
                    {
                        var p = cmd.CreateParameter(); p.ParameterName = "pfrom"; p.Value = from.Value.Date;
                        cmd.Parameters.Add(p); conditions.Add("created_at >= @pfrom");
                    }
                    if (to.HasValue)
                    {
                        var p = cmd.CreateParameter(); p.ParameterName = "pto"; p.Value = to.Value.Date.AddDays(1);
                        cmd.Parameters.Add(p); conditions.Add("created_at < @pto");
                    }
                    cmd.CommandText = $"DELETE FROM activity_logs WHERE {string.Join(" AND ", conditions)}";
                }
                return await cmd.ExecuteNonQueryAsync(ct);
            }
            finally { await _db.Database.CloseConnectionAsync(); }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Delete failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<int> DeleteSelectedActivityLogsAsync(long[] ids, Guid adminId, CancellationToken ct = default)
    {
        if (ids == null || ids.Length == 0) return 0;
        try
        {
            var conn = _db.Database.GetDbConnection();
            await _db.Database.OpenConnectionAsync(ct);
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"DELETE FROM activity_logs WHERE id IN ({string.Join(",", ids)})";
                return await cmd.ExecuteNonQueryAsync(ct);
            }
            finally { await _db.Database.CloseConnectionAsync(); }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Delete failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<int> AdminPurgeAdminDeletedDocumentsAsync(Guid adminId, CancellationToken ct = default)
    {
        var docs = await _db.Documents
            .Include(d => d.Versions)
            .Where(d => d.IsDeleted && d.DeletedByAdmin)    // admin-deleted only
            .ToListAsync(ct);

        foreach (var doc in docs)
            try { await _storage.PurgeAsync(doc.FilePath, ct); } catch { /* ignore missing files */ }

        _db.DocumentVersions.RemoveRange(docs.SelectMany(d => d.Versions));
        _db.Documents.RemoveRange(docs);
        await _db.SaveChangesAsync(ct);
        await _logger.LogAsync("PURGE_ADMIN_DELETED", "Document", null, $"Permanently deleted {docs.Count} admin-deleted document(s)", adminId, ct: ct);
        return docs.Count;
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(Guid adminId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .Where(n => n.UserId == adminId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task MarkNotificationsReadAsync(Guid adminId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.UserId == adminId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    }
}
