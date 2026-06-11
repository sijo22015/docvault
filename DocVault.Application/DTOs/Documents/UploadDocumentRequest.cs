namespace DocVault.Application.DTOs.Documents;

public record UploadDocumentRequest(
    string Title,
    string? Description,
    int DepartmentId,
    int FinancialYearId,
    int DocumentTypeId,
    string[]? Tags
);
