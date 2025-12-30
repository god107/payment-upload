namespace UploadPayments.Infrastructure.Persistence.Entities;

public sealed class PaymentUploadRow
{
    public Guid Id { get; set; }

    public Guid UploadId { get; set; }

    /// <summary>
    /// 1-based row number within the CSV (excluding header).
    /// </summary>
    public int RowNumber { get; set; }

    public string MappedFieldsJson { get; set; } = "{}";

    public string ExtrasJson { get; set; } = "{}";

    public string RawRowJson { get; set; } = "{}";

    public RowValidationStatus ValidationStatus { get; set; }

    public int ErrorCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
