using System.IO.Compression;
using System.Security.Cryptography;
using DocVault.Application.DTOs.Common;
using DocVault.Application.DTOs.Documents;
using DocVault.Application.Services;
using DocVault.Domain.Entities;
using DocVault.Domain.Interfaces;
using DocVault.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DocVault.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".webp"];

    // Server-side MIME map — never trust the browser-supplied Content-Type
    private static readonly Dictionary<string, string> KnownMimeTypes = new()
    {
        { ".pdf",  "application/pdf" },
        { ".doc",  "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".txt",  "text/plain" },
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png",  "image/png" },
        { ".webp", "image/webp" },
    };

    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        { ".pdf",  [0x25, 0x50, 0x44, 0x46] },          // %PDF
        { ".docx", [0x50, 0x4B, 0x03, 0x04] },          // PK (ZIP)
        { ".doc",  [0xD0, 0xCF, 0x11, 0xE0] },          // OLE compound doc
        { ".jpg",  [0xFF, 0xD8, 0xFF] },                 // JPEG SOI
        { ".jpeg", [0xFF, 0xD8, 0xFF] },
        { ".png",  [0x89, 0x50, 0x4E, 0x47] },          // PNG signature
        // .webp handled separately: RIFF (0-3) + WEBP (8-11)
        // .txt has no magic bytes — no entry means it passes through
    };

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifier;
    private readonly long _maxBytes;

    public DocumentService(AppDbContext db, IFileStorage storage, IActivityLogger logger, INotificationService notifier, IConfiguration config)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
        _notifier = notifier;
        _maxBytes = long.Parse(config["Storage:MaxFileSizeMB"] ?? "25") * 1024 * 1024;
    }

    public async Task<DocumentDto> UploadAsync(UploadDocumentRequest request, Stream fileStream, string originalFileName, string contentType, Guid userId, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, ct);
        if (ms.Length > _maxBytes)
            throw new InvalidOperationException($"File size exceeds the {_maxBytes / 1024 / 1024} MB limit.");

        ms.Position = 0;
        if (!ValidateMagicBytes(ms, ext))
            throw new InvalidOperationException("File content does not match its declared extension. Upload rejected.");

        // Derive MIME type server-side — ignore whatever the browser sent
        var safeContentType = KnownMimeTypes.GetValueOrDefault(ext, "application/octet-stream");

        ms.Position = 0;
        var hash = await ComputeSha256Async(ms, ct);
        ms.Position = 0;

        var fy = await _db.FinancialYears.FindAsync([request.FinancialYearId], ct)
            ?? throw new InvalidOperationException("Financial year not found.");
        if (fy.IsLocked)
            throw new InvalidOperationException("This financial year is locked. No new uploads permitted.");

        var storedName = $"{Guid.NewGuid()}{ext}";
        var subPath = Path.Combine(fy.Label, request.DepartmentId.ToString(), userId.ToString());
        var fullPath = await _storage.SaveAsync(ms, storedName, subPath, ct);

        var doc = new Document
        {
            Title = request.Title,
            Description = request.Description,
            OriginalFileName = Path.GetFileName(originalFileName),
            StoredFileName = storedName,
            FilePath = fullPath,
            ContentType = safeContentType,
            FileSizeBytes = ms.Length,
            Sha256Hash = hash,
            Status = "SUBMITTED",
            Tags = request.Tags,
            UserId = userId,
            DepartmentId = request.DepartmentId,
            FinancialYearId = request.FinancialYearId,
            DocumentTypeId = request.DocumentTypeId,
            SubmittedAt = DateTime.UtcNow
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);
        await _logger.LogAsync("UPLOAD", "Document", doc.Id.ToString(), doc.Title, userId, ct: ct);
        await _notifier.NotifyAdminsAsync("New Document Uploaded", $"A new document '{doc.Title}' has been uploaded.", ct: ct);

        return await ToDto(doc, ct);
    }

    public async Task<PagedResult<DocumentDto>> GetUserDocumentsAsync(Guid userId, int page, int pageSize, int? financialYearId, int? documentTypeId, string? searchTerm, CancellationToken ct = default)
    {
        var query = _db.Documents
            .Where(d => d.UserId == userId && !d.IsDeleted && d.DeletedAt == null)
            .AsQueryable();

        if (financialYearId.HasValue)
            query = query.Where(d => d.FinancialYearId == financialYearId.Value);

        if (documentTypeId.HasValue)
            query = query.Where(d => d.DocumentTypeId == documentTypeId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term) ||
                                     (d.Description != null && d.Description.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(d => d.User)
            .Include(d => d.Department)
            .Include(d => d.FinancialYear)
            .Include(d => d.DocumentType)
            .ToListAsync(ct);

        var dtos = items.Select(d => MapToDto(d));
        return new PagedResult<DocumentDto>(dtos, total, page, pageSize);
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id, Guid? requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var doc = await _db.Documents
            .Include(d => d.User)
            .Include(d => d.Department)
            .Include(d => d.FinancialYear)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (doc == null) return null;
        if (!isAdmin && doc.UserId != requestingUserId) return null;
        return MapToDto(doc);
    }

    public async Task<Stream> DownloadAsync(Guid id, Guid? requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var doc = await _db.Documents.FindAsync([id], ct)
            ?? throw new FileNotFoundException("Document not found.");
        if (!isAdmin && doc.UserId != requestingUserId)
            throw new UnauthorizedAccessException("Access denied.");
        return await _storage.ReadAsync(doc.FilePath, ct);
    }

    public async Task SoftDeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Document not found.");
        doc.IsDeleted = true;
        doc.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _storage.DeleteAsync(doc.FilePath, ct);
        await _logger.LogAsync("DELETE", "Document", doc.Id.ToString(), doc.Title, userId, ct: ct);
    }

    public async Task RestoreAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Document not found or not deleted.");
        var grace = 30;
        if (doc.DeletedAt.HasValue && (DateTime.UtcNow - doc.DeletedAt.Value).TotalDays > grace)
            throw new InvalidOperationException("Restore window has expired.");
        doc.IsDeleted = false;
        doc.DeletedAt = null;
        await _db.SaveChangesAsync(ct);
        await _storage.RestoreAsync(doc.FilePath, ct);
        await _logger.LogAsync("RESTORE", "Document", doc.Id.ToString(), doc.Title, userId, ct: ct);
    }

    public async Task<DocumentDto> UpdateDocumentAsync(Guid id, Guid? requestingUserId, bool isAdmin, UpdateDocumentRequest request, CancellationToken ct = default)
    {
        var query = _db.Documents.Where(d => d.Id == id && !d.IsDeleted);
        if (!isAdmin) query = query.Where(d => d.UserId == requestingUserId);

        var doc = await query
            .Include(d => d.User)
            .Include(d => d.Department)
            .Include(d => d.FinancialYear)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Document not found.");

        doc.Title = request.Title;
        doc.Description = request.Description;
        await _db.SaveChangesAsync(ct);
        await _logger.LogAsync("UPDATE", "Document", doc.Id.ToString(), doc.Title, requestingUserId ?? Guid.Empty, ct: ct);

        return MapToDto(doc);
    }

    public async Task<IEnumerable<(string FileName, string Hash, bool Mismatch)>> VerifyIntegrityAsync(int financialYearId, CancellationToken ct = default)
    {
        var docs = await _db.Documents
            .Where(d => d.FinancialYearId == financialYearId && !d.IsDeleted)
            .ToListAsync(ct);

        var results = new List<(string, string, bool)>();
        foreach (var doc in docs)
        {
            try
            {
                using var stream = await _storage.ReadAsync(doc.FilePath, ct);
                var hash = await ComputeSha256Async(stream, ct);
                results.Add((doc.OriginalFileName, doc.Sha256Hash, hash != doc.Sha256Hash));
            }
            catch
            {
                results.Add((doc.OriginalFileName, doc.Sha256Hash, true));
            }
        }
        return results;
    }

    private static bool ValidateMagicBytes(Stream stream, string ext)
    {
        // WebP: bytes 0-3 must be "RIFF" and bytes 8-11 must be "WEBP"
        // A plain RIFF check would let WAV/AVI files pass — check both markers
        if (ext == ".webp")
        {
            var header = new byte[12];
            if (stream.Read(header, 0, 12) < 12) return false;
            return header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46  // RIFF
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50; // WEBP
        }

        if (!MagicBytes.TryGetValue(ext, out var magic)) return true; // .txt — no magic bytes
        var buf = new byte[magic.Length];
        stream.ReadExactly(buf, 0, buf.Length);
        return buf.SequenceEqual(magic);
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        var bytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task<DocumentDto> ToDto(Document doc, CancellationToken ct) => MapToDto(doc);

    private static DocumentDto MapToDto(Document d) => new(
        d.Id, d.Title, d.Description, d.OriginalFileName, d.ContentType, d.FileSizeBytes,
        d.Status, d.Tags, d.UploadedAt, d.SubmittedAt, d.IsDeleted, d.DeletedAt,
        d.User?.FullName ?? "", d.Department?.Name ?? "", d.FinancialYear?.Label ?? "", d.DocumentType?.Name ?? "");
}
