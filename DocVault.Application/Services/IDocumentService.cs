using DocVault.Application.DTOs.Common;
using DocVault.Application.DTOs.Documents;

namespace DocVault.Application.Services;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(UploadDocumentRequest request, Stream fileStream, string originalFileName, string contentType, Guid userId, CancellationToken ct = default);
    Task<PagedResult<DocumentDto>> GetUserDocumentsAsync(Guid userId, int page, int pageSize, int? financialYearId, int? documentTypeId, string? searchTerm, CancellationToken ct = default);
    Task<DocumentDto?> GetByIdAsync(Guid id, Guid? requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task<Stream> DownloadAsync(Guid id, Guid? requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task RestoreAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<DocumentDto> UpdateDocumentAsync(Guid id, Guid? requestingUserId, bool isAdmin, UpdateDocumentRequest request, CancellationToken ct = default);
    Task<IEnumerable<(string FileName, string Hash, bool Mismatch)>> VerifyIntegrityAsync(int financialYearId, CancellationToken ct = default);
}
