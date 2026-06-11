namespace DocVault.Application.DTOs.Documents;

public record DocumentSearchRequest(
    string? UserId,
    int? DepartmentId,
    int? FinancialYearId,
    int? DocumentTypeId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    string? SearchTerm,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 20
);
