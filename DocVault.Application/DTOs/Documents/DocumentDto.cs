namespace DocVault.Application.DTOs.Documents;

public record DocumentDto(
    Guid Id,
    string Title,
    string? Description,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Status,
    string[]? Tags,
    DateTime UploadedAt,
    DateTime? SubmittedAt,
    bool IsDeleted,
    DateTime? DeletedAt,
    string UploaderName,
    Guid UploadedById,
    string DepartmentName,
    string FinancialYearLabel,
    string DocumentTypeName,
    bool DeletedByAdmin = false
);
