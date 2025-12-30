using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UploadPayments.Api.Contracts;
using UploadPayments.Infrastructure.Persistence;
using UploadPayments.Infrastructure.Persistence.Entities;

namespace UploadPayments.Api.Controllers;

[ApiController]
[Route("api/validation-rules")]
public sealed class ValidationRulesController(UploadPaymentsDbContext db) : ControllerBase
{
    /// <summary>
    /// Get all validation rules with optional pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ValidationRulesPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        [FromQuery] bool? enabled = null,
        CancellationToken ct = default)
    {
        if (limit < 1 || limit > 200)
            return BadRequest("Limit must be between 1 and 200");

        var query = db.ValidationRules.AsNoTracking();

        if (enabled.HasValue)
            query = query.Where(r => r.Enabled == enabled.Value);

        var total = await query.CountAsync(ct);

        var rules = await query
            .OrderBy(r => r.CreatedAtUtc)
            .Skip(offset)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rules.Count > limit;
        if (hasMore)
            rules = rules.Take(limit).ToList();

        var dtos = rules.Select(MapToDto).ToList();

        return Ok(new ValidationRulesPageDto(
            Total: total,
            Returned: dtos.Count,
            NextOffset: hasMore ? offset + limit : null,
            Rules: dtos));
    }

    /// <summary>
    /// Get a validation rule by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ValidationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var rule = await db.ValidationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (rule is null)
            return NotFound();

        return Ok(MapToDto(rule));
    }

    /// <summary>
    /// Create a new validation rule
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ValidationRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateValidationRuleDto dto, CancellationToken ct = default)
    {
        var validationError = ValidateRule(dto.Scope, dto.FieldName, dto.RuleType, dto.Parameters, dto.Severity);
        if (validationError is not null)
            return BadRequest(validationError);

        if (!TryParseScope(dto.Scope, out var scope))
            return BadRequest($"Invalid Scope: {dto.Scope}. Valid values: Row, Field");

        if (!TryParseRuleType(dto.RuleType, out var ruleType))
            return BadRequest($"Invalid RuleType: {dto.RuleType}. Valid values: Required, Regex, AllowedValues, DecimalRange, DateFormat");

        if (!TryParseSeverity(dto.Severity, out var severity))
            return BadRequest($"Invalid Severity: {dto.Severity}. Valid values: Warning, Error");

        var now = DateTime.UtcNow;
        var rule = new ValidationRule
        {
            Id = Guid.NewGuid(),
            Enabled = true,
            Scope = scope,
            FieldName = dto.FieldName,
            RuleType = ruleType,
            ParametersJson = dto.Parameters,
            Severity = severity,
            Code = dto.ErrorCode,
            MessageTemplate = dto.ErrorMessageTemplate,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.ValidationRules.Add(rule);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, MapToDto(rule));
    }

    /// <summary>
    /// Update an existing validation rule
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ValidationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateValidationRuleDto dto, CancellationToken ct = default)
    {
        var rule = await db.ValidationRules.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (rule is null)
            return NotFound();

        // Parse enums if provided
        RuleScope? scope = null;
        RuleType? ruleType = null;
        RuleSeverity? severity = null;

        if (dto.Scope is not null)
        {
            if (!TryParseScope(dto.Scope, out var parsedScope))
                return BadRequest($"Invalid Scope: {dto.Scope}. Valid values: Row, Field");
            scope = parsedScope;
        }

        if (dto.RuleType is not null)
        {
            if (!TryParseRuleType(dto.RuleType, out var parsedRuleType))
                return BadRequest($"Invalid RuleType: {dto.RuleType}. Valid values: Required, Regex, AllowedValues, DecimalRange, DateFormat");
            ruleType = parsedRuleType;
        }

        if (dto.Severity is not null)
        {
            if (!TryParseSeverity(dto.Severity, out var parsedSeverity))
                return BadRequest($"Invalid Severity: {dto.Severity}. Valid values: Warning, Error");
            severity = parsedSeverity;
        }

        // Validate parameters for the rule type (use updated or existing values)
        var effectiveRuleType = ruleType ?? rule.RuleType;
        var effectiveParameters = dto.Parameters ?? rule.ParametersJson;
        var effectiveScope = scope ?? rule.Scope;
        var effectiveFieldName = dto.FieldName ?? rule.FieldName;

        var paramsValidation = ValidateParametersForRuleType(effectiveRuleType.ToString(), effectiveParameters);
        if (paramsValidation is not null)
            return BadRequest(paramsValidation);

        // Field scope requires field name
        if (effectiveScope == RuleScope.Field && string.IsNullOrWhiteSpace(effectiveFieldName))
            return BadRequest("FieldName is required when Scope is 'Field'");

        // Apply updates
        if (dto.Enabled.HasValue)
            rule.Enabled = dto.Enabled.Value;

        if (scope.HasValue)
            rule.Scope = scope.Value;

        if (dto.FieldName is not null)
            rule.FieldName = dto.FieldName;

        if (ruleType.HasValue)
            rule.RuleType = ruleType.Value;

        if (dto.Parameters is not null)
            rule.ParametersJson = dto.Parameters;

        if (severity.HasValue)
            rule.Severity = severity.Value;

        if (dto.ErrorCode is not null)
            rule.Code = dto.ErrorCode;

        if (dto.ErrorMessageTemplate is not null)
            rule.MessageTemplate = dto.ErrorMessageTemplate;

        rule.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(MapToDto(rule));
    }

    /// <summary>
    /// Delete a validation rule
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var rule = await db.ValidationRules.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (rule is null)
            return NotFound();

        db.ValidationRules.Remove(rule);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Toggle enabled status of a validation rule
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(typeof(ValidationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct = default)
    {
        var rule = await db.ValidationRules.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (rule is null)
            return NotFound();

        rule.Enabled = !rule.Enabled;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(MapToDto(rule));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private static ValidationRuleDto MapToDto(ValidationRule rule) => new(
        Id: rule.Id,
        Enabled: rule.Enabled,
        Scope: rule.Scope.ToString(),
        FieldName: rule.FieldName,
        RuleType: rule.RuleType.ToString(),
        Parameters: rule.ParametersJson,
        Severity: rule.Severity.ToString(),
        ErrorCode: rule.Code,
        ErrorMessageTemplate: rule.MessageTemplate,
        CreatedAtUtc: rule.CreatedAtUtc,
        UpdatedAtUtc: rule.UpdatedAtUtc);

    private static string? ValidateRule(string scope, string? fieldName, string ruleType, string parameters, string severity)
    {
        // Validate scope requires field name
        if (scope.Equals("Field", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(fieldName))
            return "FieldName is required when Scope is 'Field'";

        return ValidateParametersForRuleType(ruleType, parameters);
    }

    private static string? ValidateParametersForRuleType(string ruleType, string parameters)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(parameters);
            var root = json.RootElement;

            return ruleType.ToUpperInvariant() switch
            {
                "REQUIRED" => null, // No parameters needed
                "REGEX" => ValidateRegexParams(root),
                "ALLOWEDVALUES" => ValidateAllowedValuesParams(root),
                "DECIMALRANGE" => ValidateDecimalRangeParams(root),
                "DATEFORMAT" => ValidateDateFormatParams(root),
                _ => null // Unknown rule type - will be caught by enum parsing
            };
        }
        catch (System.Text.Json.JsonException)
        {
            return "Parameters must be valid JSON";
        }
    }

    private static string? ValidateRegexParams(System.Text.Json.JsonElement root)
    {
        if (!root.TryGetProperty("pattern", out var pattern) || pattern.ValueKind != System.Text.Json.JsonValueKind.String)
            return "Regex rule requires 'pattern' string parameter";

        try
        {
            _ = new System.Text.RegularExpressions.Regex(pattern.GetString()!);
        }
        catch
        {
            return "Invalid regex pattern";
        }

        return null;
    }

    private static string? ValidateAllowedValuesParams(System.Text.Json.JsonElement root)
    {
        if (!root.TryGetProperty("values", out var values) || values.ValueKind != System.Text.Json.JsonValueKind.Array)
            return "AllowedValues rule requires 'values' array parameter";

        if (values.GetArrayLength() == 0)
            return "AllowedValues 'values' array cannot be empty";

        return null;
    }

    private static string? ValidateDecimalRangeParams(System.Text.Json.JsonElement root)
    {
        var hasMin = root.TryGetProperty("min", out var min) && min.ValueKind == System.Text.Json.JsonValueKind.Number;
        var hasMax = root.TryGetProperty("max", out var max) && max.ValueKind == System.Text.Json.JsonValueKind.Number;

        if (!hasMin && !hasMax)
            return "DecimalRange rule requires at least 'min' or 'max' number parameter";

        return null;
    }

    private static string? ValidateDateFormatParams(System.Text.Json.JsonElement root)
    {
        if (!root.TryGetProperty("format", out var format) || format.ValueKind != System.Text.Json.JsonValueKind.String)
            return "DateFormat rule requires 'format' string parameter";

        return null;
    }

    private static bool TryParseScope(string value, out RuleScope scope) =>
        Enum.TryParse(value, ignoreCase: true, out scope);

    private static bool TryParseRuleType(string value, out RuleType ruleType) =>
        Enum.TryParse(value, ignoreCase: true, out ruleType);

    private static bool TryParseSeverity(string value, out RuleSeverity severity) =>
        Enum.TryParse(value, ignoreCase: true, out severity);
}
