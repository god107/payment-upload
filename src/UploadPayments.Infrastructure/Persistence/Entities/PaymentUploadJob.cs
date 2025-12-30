using System.ComponentModel.DataAnnotations;

namespace UploadPayments.Infrastructure.Persistence.Entities;

public sealed class PaymentUploadJob
{
    public Guid Id { get; set; }

    public Guid UploadId { get; set; }

    public JobType JobType { get; set; }

    public JobStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime NextRunAtUtc { get; set; }

    public DateTime? LockedAtUtc { get; set; }

    [MaxLength(128)]
    public string? LockedBy { get; set; }

    public DateTime? HeartbeatAtUtc { get; set; }

    [MaxLength(2048)]
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
