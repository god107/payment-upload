using Microsoft.EntityFrameworkCore;
using UploadPayments.Infrastructure.Persistence;
using UploadPayments.Infrastructure.Persistence.Entities;
using UploadPayments.Contracts;

namespace UploadPayments.Api.HostedServices;

public sealed class DatabaseBootstrapper(IServiceScopeFactory scopeFactory, ILogger<DatabaseBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UploadPaymentsDbContext>();

        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync(cancellationToken);

        await SeedRulesAsync(db, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedRulesAsync(UploadPaymentsDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var anyRules = await db.ValidationRules.AnyAsync(ct);
        if (anyRules)
        {
            return;
        }

        // Minimal starter rules for the sample file.
        db.ValidationRules.AddRange(
            Required("BeneficiaryName", "BENEFICIARY_REQUIRED", "{FieldName} is required"),
            Required("UniqueReference", "UNIQUE_REFERENCE_REQUIRED", "{FieldName} is required"),
            Required("PaymentCurrency", "PAYMENT_CCY_REQUIRED", "{FieldName} is required"),
            Required("Amount", "AMOUNT_REQUIRED", "{FieldName} is required"),
            Required("InvoiceDate", "INVOICE_DATE_REQUIRED", "{FieldName} is required"),

            new ValidationRule
            {
                Id = Guid.NewGuid(),
                Enabled = true,
                Scope = RuleScope.Field,
                FieldName = "PaymentCurrency",
                RuleType = RuleType.Regex,
                ParametersJson = "{\"pattern\":\"^[A-Z]{3}$\",\"ignoreCase\":true}",
                Severity = RuleSeverity.Error,
                Code = "PAYMENT_CCY_FORMAT",
                MessageTemplate = "{FieldName} must be a 3-letter currency code",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },

            new ValidationRule
            {
                Id = Guid.NewGuid(),
                Enabled = true,
                Scope = RuleScope.Field,
                FieldName = "Amount",
                RuleType = RuleType.DecimalRange,
                ParametersJson = "{\"min\":0.01}",
                Severity = RuleSeverity.Error,
                Code = "AMOUNT_MIN",
                MessageTemplate = "{FieldName} must be > 0",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },

            new ValidationRule
            {
                Id = Guid.NewGuid(),
                Enabled = true,
                Scope = RuleScope.Field,
                FieldName = "InvoiceDate",
                RuleType = RuleType.DateFormat,
                ParametersJson = "{\"format\":\"dd.MM.yyyy\"}",
                Severity = RuleSeverity.Error,
                Code = "INVOICE_DATE_FORMAT",
                MessageTemplate = "{FieldName} must match dd.MM.yyyy",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        await db.SaveChangesAsync(ct);

        static ValidationRule Required(string fieldName, string code, string message)
        {
            var now = DateTime.UtcNow;
            return new ValidationRule
            {
                Id = Guid.NewGuid(),
                Enabled = true,
                Scope = RuleScope.Field,
                FieldName = fieldName,
                RuleType = RuleType.Required,
                ParametersJson = "{}",
                Severity = RuleSeverity.Error,
                Code = code,
                MessageTemplate = message,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }
    }
}
