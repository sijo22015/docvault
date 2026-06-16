using DocVault.Application.DTOs.Admin;
using DocVault.Application.DTOs.Common;
using DocVault.Application.DTOs.Documents;

namespace DocVault.Application.Services;

public interface IAdminService
{
    Task<PagedResult<UserDto>> GetUsersAsync(string? status, int page, int pageSize, CancellationToken ct = default);
    Task ApproveUserAsync(Guid userId, Guid adminId, CancellationToken ct = default);
    Task RevokeUserAsync(Guid userId, Guid adminId, CancellationToken ct = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default);
    Task<AnalyticsDto> GetAnalyticsAsync(string financialYear, CancellationToken ct = default);
    Task<PagedResult<DocumentDto>> SearchDocumentsAsync(DocumentSearchRequest request, CancellationToken ct = default);
    Task LockFinancialYearAsync(int fyId, Guid adminId, CancellationToken ct = default);
    Task<byte[]> ExportFinancialYearZipAsync(int fyId, CancellationToken ct = default);
    Task<(byte[] Data, string FileName)> MergeDocumentsToPdfAsync(MergePdfRequest request, CancellationToken ct = default);
    Task AdminRestoreDocumentAsync(Guid documentId, Guid adminId, CancellationToken ct = default);
    Task<int> AdminPurgeDeletedDocumentsAsync(Guid adminId, CancellationToken ct = default);
}
