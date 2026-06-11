namespace DocVault.Application.DTOs.Admin;

public record DashboardSummaryDto(
    int TotalUsers,
    int PendingApprovals,
    int TotalDocuments,
    int DocumentsThisMonth,
    IList<DeptDocCount> ByDepartment,
    IList<RecentActivity> RecentActivity
);

public record DeptDocCount(string Department, int Count);
public record RecentActivity(string Action, string UserName, string EntityType, DateTime CreatedAt);
