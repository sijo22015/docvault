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
    string? UploaderName,
    bool IncludeDeleted = false,
    bool OnlyDeleted = false,
    bool? OnlyDeletedByAdmin = null,
    bool? OnlyDeletedBySecAdmin = null,
    int Page = 1,
    int PageSize = 20
);
