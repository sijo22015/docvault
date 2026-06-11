namespace DocVault.Application.DTOs.Admin;

public record AnalyticsDto(
    string FinancialYear,
    IList<DeptProgress> DepartmentProgress,
    IList<MonthlyTrend> MonthlyTrend,
    IList<TopContributor> TopContributors
);

public record DeptProgress(string Department, int Required, int Submitted, double PercentComplete);
public record MonthlyTrend(string Month, int Count);
public record TopContributor(string UserName, string Department, int DocumentCount);
