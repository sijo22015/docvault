using DocVault.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<FinancialYear> FinancialYears => Set<FinancialYear>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<RequiredDocument> RequiredDocuments => Set<RequiredDocument>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        builder.Entity<FinancialYear>(e =>
        {
            e.ToTable("financial_years");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.Label).HasColumnName("label").HasMaxLength(20).IsRequired();
            e.Property(x => x.FyStart).HasColumnName("fy_start");
            e.Property(x => x.FyEnd).HasColumnName("fy_end");
            e.Property(x => x.IsLocked).HasColumnName("is_locked").HasDefaultValue(false);
            e.Property(x => x.LockedAt).HasColumnName("locked_at");
            e.Property(x => x.LockedBy).HasColumnName("locked_by");
        });

        builder.Entity<DocumentType>(e =>
        {
            e.ToTable("document_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });

        builder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.OriginalFileName).HasColumnName("original_filename").HasMaxLength(500).IsRequired();
            e.Property(x => x.StoredFileName).HasColumnName("stored_filename").HasMaxLength(500).IsRequired();
            e.Property(x => x.FilePath).HasColumnName("file_path").IsRequired();
            e.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(200).IsRequired();
            e.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();
            e.Property(x => x.Sha256Hash).HasColumnName("sha256_hash").HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("DRAFT");
            e.Property(x => x.Tags).HasColumnName("tags").HasColumnType("text[]");
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            e.Property(x => x.DeletedByAdmin).HasColumnName("deleted_by_admin").HasDefaultValue(false);
            e.Property(x => x.DeletedBySecAdmin).HasColumnName("deleted_by_sec_admin").HasDefaultValue(false);
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
            e.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.FinancialYearId).HasColumnName("financial_year_id");
            e.Property(x => x.DocumentTypeId).HasColumnName("document_type_id");
            e.HasOne(x => x.User).WithMany(u => u.Documents).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Department).WithMany(d => d.Documents).HasForeignKey(x => x.DepartmentId);
            e.HasOne(x => x.FinancialYear).WithMany(f => f.Documents).HasForeignKey(x => x.FinancialYearId);
            e.HasOne(x => x.DocumentType).WithMany(t => t.Documents).HasForeignKey(x => x.DocumentTypeId);
        });

        builder.Entity<DocumentVersion>(e =>
        {
            e.ToTable("document_versions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.DocumentId).HasColumnName("document_id");
            e.Property(x => x.StoredFileName).HasColumnName("stored_filename").HasMaxLength(500).IsRequired();
            e.Property(x => x.FilePath).HasColumnName("file_path").IsRequired();
            e.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
            e.Property(x => x.Sha256Hash).HasColumnName("sha256_hash").HasMaxLength(64);
            e.Property(x => x.VersionNumber).HasColumnName("version_number");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.HasOne(x => x.Document).WithMany(d => d.Versions).HasForeignKey(x => x.DocumentId);
        });

        builder.Entity<RequiredDocument>(e =>
        {
            e.ToTable("required_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.DocumentTypeId).HasColumnName("document_type_id");
            e.Property(x => x.FinancialYearId).HasColumnName("financial_year_id");
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.HasOne(x => x.Department).WithMany(d => d.RequiredDocuments).HasForeignKey(x => x.DepartmentId);
            e.HasOne(x => x.DocumentType).WithMany(t => t.RequiredDocuments).HasForeignKey(x => x.DocumentTypeId);
            e.HasOne(x => x.FinancialYear).WithMany(f => f.RequiredDocuments).HasForeignKey(x => x.FinancialYearId);
        });

        builder.Entity<ActivityLog>(e =>
        {
            e.ToTable("activity_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(200);
            e.Property(x => x.Details).HasColumnName("details");
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User).WithMany(u => u.ActivityLogs).HasForeignKey(x => x.UserId).IsRequired(false);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(88).IsRequired();
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.IsRevoked).HasColumnName("is_revoked").HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            e.Property(x => x.ReplacedByToken).HasColumnName("replaced_by_token").HasMaxLength(88);
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);
        });

        builder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            e.Property(x => x.Message).HasColumnName("message").IsRequired();
            e.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);
            e.Property(x => x.Type).HasColumnName("type").HasMaxLength(30).HasDefaultValue("INFO");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ReadAt).HasColumnName("read_at");
            e.HasOne(x => x.User).WithMany(u => u.Notifications).HasForeignKey(x => x.UserId);
        });

        // AppUser additional columns
        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.FullName).HasColumnName("FullName").HasMaxLength(200);
            e.Property(x => x.Department).HasColumnName("Department").HasMaxLength(100);
            e.Property(x => x.UserStatus).HasColumnName("UserStatus").HasMaxLength(30);
            e.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            e.Property(x => x.LastLoginAt).HasColumnName("LastLoginAt");
            e.Property(x => x.RevokedAt).HasColumnName("RevokedAt");
            e.Property(x => x.RevokedBy).HasColumnName("RevokedBy").HasMaxLength(256);
            e.Property(x => x.MobileNumber).HasColumnName("MobileNumber").HasMaxLength(20);
            e.Property(x => x.WhatsAppNumber).HasColumnName("WhatsAppNumber").HasMaxLength(20);
            e.Property(x => x.ProfilePhotoPath).HasColumnName("ProfilePhotoPath").HasMaxLength(500);
        });
    }
}
