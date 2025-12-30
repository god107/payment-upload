using Microsoft.EntityFrameworkCore;
using UploadPayments.Infrastructure.Persistence.Entities;

namespace UploadPayments.Infrastructure.Persistence;

public sealed class UploadPaymentsDbContext : DbContext
{
    public UploadPaymentsDbContext(DbContextOptions<UploadPaymentsDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentUpload> PaymentUploads => Set<PaymentUpload>();
    public DbSet<PaymentUploadJob> PaymentUploadJobs => Set<PaymentUploadJob>();
    public DbSet<PaymentUploadChunk> PaymentUploadChunks => Set<PaymentUploadChunk>();
    public DbSet<PaymentUploadRow> PaymentUploadRows => Set<PaymentUploadRow>();
    public DbSet<PaymentUploadRowError> PaymentUploadRowErrors => Set<PaymentUploadRowError>();
    public DbSet<ValidationRule> ValidationRules => Set<ValidationRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<PaymentUpload>(b =>
        {
            b.ToTable("payment_uploads");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Token).HasColumnName("token");
            b.HasIndex(x => x.Token).IsUnique();

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            b.Property(x => x.OriginalFileName).HasColumnName("original_file_name");
            b.Property(x => x.ContentType).HasColumnName("content_type");
            b.Property(x => x.SizeBytes).HasColumnName("size_bytes");
            b.Property(x => x.ContentSha256).HasColumnName("content_sha256");

            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.LastError).HasColumnName("last_error");

            b.Property(x => x.TotalRows).HasColumnName("total_rows");
            b.Property(x => x.ProcessedRows).HasColumnName("processed_rows");
            b.Property(x => x.FailedRows).HasColumnName("failed_rows");
            b.Property(x => x.SucceededRows).HasColumnName("succeeded_rows");

            b.Property(x => x.RawCsvBytes).HasColumnName("raw_csv_bytes");
            b.Property(x => x.HeadersJson).HasColumnName("headers_json");
        });

        modelBuilder.Entity<PaymentUploadJob>(b =>
        {
            b.ToTable("payment_upload_jobs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UploadId).HasColumnName("upload_id");
            b.HasIndex(x => new { x.Status, x.NextRunAtUtc });

            b.Property(x => x.JobType).HasColumnName("job_type");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.AttemptCount).HasColumnName("attempt_count");
            b.Property(x => x.NextRunAtUtc).HasColumnName("next_run_at_utc");

            b.Property(x => x.LockedAtUtc).HasColumnName("locked_at_utc");
            b.Property(x => x.LockedBy).HasColumnName("locked_by");
            b.Property(x => x.HeartbeatAtUtc).HasColumnName("heartbeat_at_utc");

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            b.Property(x => x.LastError).HasColumnName("last_error");

            b.HasOne<PaymentUpload>()
                .WithMany()
                .HasForeignKey(x => x.UploadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentUploadChunk>(b =>
        {
            b.ToTable("payment_upload_chunks");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");

            b.Property(x => x.UploadId).HasColumnName("upload_id");
            b.HasIndex(x => new { x.UploadId, x.ChunkIndex }).IsUnique();

            b.Property(x => x.ChunkIndex).HasColumnName("chunk_index");
            b.Property(x => x.RowStart).HasColumnName("row_start");
            b.Property(x => x.RowEnd).HasColumnName("row_end");

            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.AttemptCount).HasColumnName("attempt_count");
            b.Property(x => x.NextRunAtUtc).HasColumnName("next_run_at_utc");
            b.Property(x => x.LockedAtUtc).HasColumnName("locked_at_utc");
            b.Property(x => x.LockedBy).HasColumnName("locked_by");
            b.Property(x => x.HeartbeatAtUtc).HasColumnName("heartbeat_at_utc");
            b.Property(x => x.LastError).HasColumnName("last_error");

            b.Property(x => x.ProcessedRows).HasColumnName("processed_rows");
            b.Property(x => x.FailedRows).HasColumnName("failed_rows");
            b.Property(x => x.SucceededRows).HasColumnName("succeeded_rows");

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            b.HasOne<PaymentUpload>()
                .WithMany()
                .HasForeignKey(x => x.UploadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentUploadRow>(b =>
        {
            b.ToTable("payment_upload_rows");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");

            b.Property(x => x.UploadId).HasColumnName("upload_id");
            b.Property(x => x.RowNumber).HasColumnName("row_number");
            b.HasIndex(x => new { x.UploadId, x.RowNumber }).IsUnique();

            b.Property(x => x.MappedFieldsJson).HasColumnName("mapped_fields_json");
            b.Property(x => x.ExtrasJson).HasColumnName("extras_json");
            b.Property(x => x.RawRowJson).HasColumnName("raw_row_json");

            b.Property(x => x.ValidationStatus).HasColumnName("validation_status");
            b.Property(x => x.ErrorCount).HasColumnName("error_count");

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            b.HasOne<PaymentUpload>()
                .WithMany()
                .HasForeignKey(x => x.UploadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentUploadRowError>(b =>
        {
            b.ToTable("payment_upload_row_errors");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");

            b.Property(x => x.UploadId).HasColumnName("upload_id");
            b.Property(x => x.RowNumber).HasColumnName("row_number");
            b.HasIndex(x => new { x.UploadId, x.RowNumber });
            b.HasIndex(x => new { x.UploadId, x.IsError, x.RowNumber });

            b.Property(x => x.FieldName).HasColumnName("field_name");
            b.Property(x => x.Code).HasColumnName("code");
            b.Property(x => x.Message).HasColumnName("message");
            b.Property(x => x.Severity).HasColumnName("severity");
            b.Property(x => x.IsError).HasColumnName("is_error");
            b.Property(x => x.RuleId).HasColumnName("rule_id");

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");

            b.HasOne<PaymentUpload>()
                .WithMany()
                .HasForeignKey(x => x.UploadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ValidationRule>(b =>
        {
            b.ToTable("validation_rules");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");

            b.Property(x => x.Enabled).HasColumnName("enabled");
            b.Property(x => x.Scope).HasColumnName("scope");
            b.Property(x => x.FieldName).HasColumnName("field_name");
            b.Property(x => x.RuleType).HasColumnName("rule_type");
            b.Property(x => x.ParametersJson).HasColumnName("parameters_json");
            b.Property(x => x.Severity).HasColumnName("severity");
            b.Property(x => x.Code).HasColumnName("code");
            b.Property(x => x.MessageTemplate).HasColumnName("message_template");

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });
    }
}
