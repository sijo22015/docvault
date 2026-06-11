using System.Text.Json.Serialization;

namespace DocVault.Application.DTOs.Documents;

public class MergePdfRequest
{
    [JsonPropertyName("documentIds")]
    public string[]? DocumentIds { get; set; }

    [JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; set; }

    [JsonPropertyName("departmentId")]
    public int? DepartmentId { get; set; }

    [JsonPropertyName("financialYearId")]
    public int? FinancialYearId { get; set; }

    [JsonPropertyName("documentTypeId")]
    public int? DocumentTypeId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
