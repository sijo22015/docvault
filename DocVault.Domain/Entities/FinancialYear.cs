namespace DocVault.Domain.Entities;

public class FinancialYear
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty; // e.g. "2025-2026"
    public DateOnly FyStart { get; set; }
    public DateOnly FyEnd { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<RequiredDocument> RequiredDocuments { get; set; } = new List<RequiredDocument>();
}
