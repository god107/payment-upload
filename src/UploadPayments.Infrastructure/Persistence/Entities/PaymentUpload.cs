using System.ComponentModel.DataAnnotations;

namespace UploadPayments.Infrastructure.Persistence.Entities;

public sealed class PaymentUpload
{
    public Guid Id { get; set; }

    public Guid Token { get; set; }

    [MaxLength(512)]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    [MaxLength(64)]
    public string ContentSha256 { get; set; } = string.Empty;

    public UploadStatus Status { get; set; }

    [MaxLength(2048)]
    public string? LastError { get; set; }

    public int? TotalRows { get; set; }

    public int ProcessedRows { get; set; }

    public int SucceededRows { get; set; }

    public int FailedRows { get; set; }

    public byte[] RawCsvBytes { get; set; } = Array.Empty<byte>();

    public string? HeadersJson { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
