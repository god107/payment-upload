using System.ComponentModel.DataAnnotations;

namespace UploadPayments.Infrastructure.Persistence.Entities;

public sealed class PaymentUploadRowError
{
    public Guid Id { get; set; }

    public Guid UploadId { get; set; }

    public int RowNumber { get; set; }

    [MaxLength(128)]
    public string? FieldName { get; set; }

    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string Message { get; set; } = string.Empty;

    public RuleSeverity Severity { get; set; }

    public bool IsError { get; set; }

    public Guid? RuleId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
