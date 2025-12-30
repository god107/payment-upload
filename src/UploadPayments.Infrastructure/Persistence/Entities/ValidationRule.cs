using System.ComponentModel.DataAnnotations;

namespace UploadPayments.Infrastructure.Persistence.Entities;

public sealed class ValidationRule
{
    public Guid Id { get; set; }

    public bool Enabled { get; set; }

    public RuleScope Scope { get; set; }

    [MaxLength(128)]
    public string? FieldName { get; set; }

    public RuleType RuleType { get; set; }

    public string ParametersJson { get; set; } = "{}";

    public RuleSeverity Severity { get; set; }

    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string MessageTemplate { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
