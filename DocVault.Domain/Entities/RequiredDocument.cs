namespace DocVault.Domain.Entities;

public class RequiredDocument
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = null!;

    public int FinancialYearId { get; set; }
    public FinancialYear FinancialYear { get; set; } = null!;

    public DateTime? DueDate { get; set; }
    public bool IsActive { get; set; } = true;
}
