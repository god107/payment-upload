using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using UploadPayments.Infrastructure.Persistence;
using UploadPayments.Infrastructure.Persistence.Entities;
using UploadPayments.Contracts;

namespace UploadPayments.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<ValidationWorkerOptions> optionsAccessor,
    IPaymentUploadNotificationService notificationService) : BackgroundService
{
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";
    private readonly ValidationWorkerOptions _options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Validation worker started: {WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<UploadPaymentsDbContext>();

                var didWork = await TryRunParseJobAsync(db, stoppingToken);
                didWork |= await TryRunChunkAsync(db, stoppingToken);

                if (!didWork)
                {
                    await Task.Delay(_options.PollDelayMilliseconds, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker loop error");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task<bool> TryRunParseJobAsync(UploadPaymentsDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        PaymentUploadJob? job;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            job = await db.PaymentUploadJobs
                .FromSqlRaw(
                    """
                    SELECT *
                    FROM payment_upload_jobs
                    WHERE status = 0
                      AND job_type = 0
                      AND next_run_at_utc <= now()
                    ORDER BY next_run_at_utc
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                    """)
                .OrderBy(x => x.NextRunAtUtc)
                .FirstOrDefaultAsync(ct);

            if (job is null)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            job.Status = JobStatus.Running;
            job.LockedAtUtc = now;
            job.LockedBy = _workerId;
            job.HeartbeatAtUtc = now;
            job.UpdatedAtUtc = now;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        try
        {
            await RunParseJobAsync(db, job, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Parse job failed: {JobId}", job.Id);
            await FailJobAsync(db, job, ex.Message, ct);
            return true;
        }
    }

    private async Task RunParseJobAsync(UploadPaymentsDbContext db, PaymentUploadJob job, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var upload = await db.PaymentUploads.FirstOrDefaultAsync(x => x.Id == job.UploadId, ct);
        if (upload is null)
        {
            await FailJobAsync(db, job, "Upload not found", ct);
            return;
        }

        var now = DateTime.UtcNow;
        upload.Status = UploadStatus.Parsing;
        upload.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);

        // Notify clients that parsing has started
        await notificationService.NotifyUploadStatusChanged(upload.Id, upload.Token, "Parsing");

        // Skip parsing if already parsed.
        var alreadyHasChunks = await db.PaymentUploadChunks.AnyAsync(x => x.UploadId == upload.Id, ct);
        if (alreadyHasChunks)
        {
            await SucceedJobAsync(db, job, ct);
            return;
        }

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            DetectDelimiter = true,
            TrimOptions = TrimOptions.Trim
        };

        var headerRecord = Array.Empty<string>();
        var rowNumber = 0;
        var batch = new List<PaymentUploadRow>(capacity: 512);

        await using (var ms = new MemoryStream(upload.RawCsvBytes, writable: false))
        using (var reader = new StreamReader(ms, leaveOpen: true))
        using (var csv = new CsvReader(reader, csvConfig))
        {
            if (!await csv.ReadAsync())
            {
                throw new InvalidOperationException("CSV is empty.");
            }

            csv.ReadHeader();
            headerRecord = csv.HeaderRecord ?? Array.Empty<string>();

            while (await csv.ReadAsync())
            {
                rowNumber++;

                var raw = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in headerRecord)
                {
                    raw[NormalizeHeader(header)] = csv.GetField(header);
                }

                var rawJson = JsonSerializer.Serialize(raw);

                batch.Add(new PaymentUploadRow
                {
                    Id = Guid.NewGuid(),
                    UploadId = upload.Id,
                    RowNumber = rowNumber,
                    MappedFieldsJson = rawJson,
                    ExtrasJson = "{}",
                    RawRowJson = rawJson,
                    ValidationStatus = RowValidationStatus.Pending,
                    ErrorCount = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });

                if (batch.Count >= 500)
                {
                    db.PaymentUploadRows.AddRange(batch);
                    await db.SaveChangesAsync(ct);
                    batch.Clear();
                }
            }
        }

        if (batch.Count > 0)
        {
            db.PaymentUploadRows.AddRange(batch);
            await db.SaveChangesAsync(ct);
        }

        upload.TotalRows = rowNumber;
        upload.HeadersJson = JsonSerializer.Serialize(headerRecord.Select(NormalizeHeader));
        upload.Status = UploadStatus.Validating;
        upload.UpdatedAtUtc = DateTime.UtcNow;

        var chunkSize = Math.Max(1, _options.ChunkSizeRows);
        var chunkIndex = 0;
        for (var start = 1; start <= rowNumber; start += chunkSize)
        {
            var end = Math.Min(rowNumber, start + chunkSize - 1);
            db.PaymentUploadChunks.Add(new PaymentUploadChunk
            {
                Id = Guid.NewGuid(),
                UploadId = upload.Id,
                ChunkIndex = chunkIndex++,
                RowStart = start,
                RowEnd = end,
                Status = ChunkStatus.Queued,
                AttemptCount = 0,
                NextRunAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);

        // Notify clients that validation has started
        await notificationService.NotifyUploadStatusChanged(upload.Id, upload.Token, "Validating", rowNumber, chunkIndex);

        await SucceedJobAsync(db, job, ct);
        logger.LogInformation("Parsed upload {UploadId}: {TotalRows} rows, {Chunks} chunks in {ElapsedMs} ms", upload.Id, rowNumber, chunkIndex, sw.Elapsed.TotalMilliseconds);
    }

    private async Task<bool> TryRunChunkAsync(UploadPaymentsDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var staleLockBefore = now.AddSeconds(-Math.Max(5, _options.StaleLockSeconds));

        PaymentUploadChunk? chunk;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            // Requeue stale running chunks
            var stale = await db.PaymentUploadChunks
                .Where(x => x.Status == ChunkStatus.Running && x.HeartbeatAtUtc != null && x.HeartbeatAtUtc < staleLockBefore)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, ChunkStatus.Queued)
                    .SetProperty(x => x.LockedAtUtc, (DateTime?)null)
                    .SetProperty(x => x.LockedBy, (string?)null)
                    .SetProperty(x => x.HeartbeatAtUtc, (DateTime?)null)
                    .SetProperty(x => x.NextRunAtUtc, DateTime.UtcNow)
                    .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);

            if (stale > 0)
            {
                logger.LogWarning("Re-queued {Count} stale chunks", stale);
            }

            chunk = await db.PaymentUploadChunks
                .FromSqlRaw(
                    """
                    SELECT *
                    FROM payment_upload_chunks
                    WHERE status = 0
                      AND next_run_at_utc <= now()
                    ORDER BY upload_id, chunk_index
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                    """)
                .OrderBy(x => x.UploadId)
                .ThenBy(x => x.ChunkIndex)
                .FirstOrDefaultAsync(ct);

            if (chunk is null)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            chunk.Status = ChunkStatus.Running;
            chunk.LockedAtUtc = now;
            chunk.LockedBy = _workerId;
            chunk.HeartbeatAtUtc = now;
            chunk.UpdatedAtUtc = now;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        try
        {
            await RunChunkAsync(db, chunk, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chunk failed: {ChunkId} upload={UploadId} range={Start}-{End}", chunk.Id, chunk.UploadId, chunk.RowStart, chunk.RowEnd);

            await FailChunkAsync(db, chunk, ex.Message, ct);
            return true;
        }
    }

    private async Task RunChunkAsync(UploadPaymentsDbContext db, PaymentUploadChunk chunk, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var now = DateTime.UtcNow;

        var upload = await db.PaymentUploads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == chunk.UploadId, ct);
        if (upload is null)
        {
            throw new InvalidOperationException("Upload not found.");
        }

        var rules = await db.ValidationRules
            .AsNoTracking()
            .Where(x => x.Enabled)
            .ToListAsync(ct);

        var rows = await db.PaymentUploadRows
            .Where(x => x.UploadId == chunk.UploadId)
            .Where(x => x.RowNumber >= chunk.RowStart && x.RowNumber <= chunk.RowEnd)
            .OrderBy(x => x.RowNumber)
            .ToListAsync(ct);

        var processed = 0;
        var failed = 0;
        var succeeded = 0;

        var pendingErrors = new List<PaymentUploadRowError>(capacity: 512);

        foreach (var row in rows)
        {
            processed++;
            chunk.HeartbeatAtUtc = DateTime.UtcNow;

            var fields = JsonSerializer.Deserialize<Dictionary<string, string?>>(row.MappedFieldsJson)
                         ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            var errorCount = 0;

            foreach (var rule in rules)
            {
                if (rule.Scope == RuleScope.Field && string.IsNullOrWhiteSpace(rule.FieldName))
                {
                    continue;
                }

                string? fieldName = rule.FieldName;
                string? rawValue = null;

                if (rule.Scope == RuleScope.Field)
                {
                    fields.TryGetValue(NormalizeHeader(fieldName!), out rawValue);
                }

                var failures = EvaluateRule(rule, fieldName, rawValue);
                foreach (var failure in failures)
                {
                    var isError = failure.Severity == RuleSeverity.Error;
                    if (isError)
                    {
                        errorCount++;
                    }

                    pendingErrors.Add(new PaymentUploadRowError
                    {
                        Id = Guid.NewGuid(),
                        UploadId = row.UploadId,
                        RowNumber = row.RowNumber,
                        FieldName = failure.FieldName,
                        Code = failure.Code,
                        Message = failure.Message,
                        Severity = failure.Severity,
                        IsError = isError,
                        RuleId = failure.RuleId,
                        CreatedAtUtc = now
                    });
                }
            }

            row.ErrorCount = errorCount;
            row.ValidationStatus = errorCount == 0 ? RowValidationStatus.Valid : RowValidationStatus.Invalid;
            row.UpdatedAtUtc = DateTime.UtcNow;

            if (errorCount == 0)
            {
                succeeded++;
            }
            else
            {
                failed++;
            }

            if (pendingErrors.Count >= 1000)
            {
                db.PaymentUploadRowErrors.AddRange(pendingErrors);
                await db.SaveChangesAsync(ct);
                pendingErrors.Clear();
            }

            if (processed % 200 == 0)
            {
                await db.SaveChangesAsync(ct);
            }
            
            // Send real-time progress updates every 100 rows
            // Calculate cumulative totals from completed chunks + current chunk progress
            if (processed % 100 == 0)
            {
                var completedChunksTotals = await db.PaymentUploadChunks
                    .AsNoTracking()
                    .Where(x => x.UploadId == chunk.UploadId && x.Status == ChunkStatus.Succeeded)
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        TotalProcessed = g.Sum(x => x.ProcessedRows),
                        TotalSucceeded = g.Sum(x => x.SucceededRows),
                        TotalFailed = g.Sum(x => x.FailedRows)
                    })
                    .FirstOrDefaultAsync(ct);
                
                // Add current chunk's progress to completed chunks
                var cumulativeProcessed = (completedChunksTotals?.TotalProcessed ?? 0) + processed;
                var cumulativeSucceeded = (completedChunksTotals?.TotalSucceeded ?? 0) + succeeded;
                var cumulativeFailed = (completedChunksTotals?.TotalFailed ?? 0) + failed;
                
                var uploadToken = upload.Token;
                await notificationService.NotifyRowProgressUpdate(chunk.UploadId, uploadToken, chunk.ChunkIndex, cumulativeProcessed, cumulativeSucceeded);
            }
        }

        if (pendingErrors.Count > 0)
        {
            db.PaymentUploadRowErrors.AddRange(pendingErrors);
            await db.SaveChangesAsync(ct);
        }

        chunk.ProcessedRows = processed;
        chunk.SucceededRows = succeeded;
        chunk.FailedRows = failed;
        chunk.Status = ChunkStatus.Succeeded;
        chunk.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // Count completed chunks for progress reporting
        var totalChunks = await db.PaymentUploadChunks.CountAsync(x => x.UploadId == chunk.UploadId, ct);
        var completedChunks = await db.PaymentUploadChunks.CountAsync(x => x.UploadId == chunk.UploadId && x.Status == ChunkStatus.Succeeded, ct);
        
        // Calculate cumulative totals across all completed chunks
        var cumulativeTotals = await db.PaymentUploadChunks
            .AsNoTracking()
            .Where(x => x.UploadId == chunk.UploadId && x.Status == ChunkStatus.Succeeded)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalProcessed = g.Sum(x => x.ProcessedRows),
                TotalSucceeded = g.Sum(x => x.SucceededRows),
                TotalFailed = g.Sum(x => x.FailedRows)
            })
            .FirstOrDefaultAsync(ct);
        
        // Get upload token for notification
        var uploadForNotification = await db.PaymentUploads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == chunk.UploadId, ct);
        if (uploadForNotification is not null && cumulativeTotals is not null)
        {
            // Send cumulative totals instead of single chunk stats
            await notificationService.NotifyChunkCompleted(
                chunk.UploadId, 
                uploadForNotification.Token, 
                chunk.ChunkIndex, 
                cumulativeTotals.TotalProcessed, 
                cumulativeTotals.TotalSucceeded, 
                cumulativeTotals.TotalFailed, 
                totalChunks, 
                completedChunks);
        }

        await TryFinalizeUploadAsync(db, chunk.UploadId, ct);

        logger.LogInformation(
            "Chunk {ChunkId} upload={UploadId} rows {Start}-{End} processed={Processed} succeeded={Succeeded} failed={Failed} in {ElapsedMs} ms",
            chunk.Id,
            chunk.UploadId,
            chunk.RowStart,
            chunk.RowEnd,
            processed,
            succeeded,
            failed,
            sw.Elapsed.TotalMilliseconds);
    }

    private async Task TryFinalizeUploadAsync(UploadPaymentsDbContext db, Guid uploadId, CancellationToken ct)
    {
        // Only finalize when no queued/running chunks remain.
        var remaining = await db.PaymentUploadChunks
            .AsNoTracking()
            .Where(x => x.UploadId == uploadId)
            .Where(x => x.Status == ChunkStatus.Queued || x.Status == ChunkStatus.Running)
            .AnyAsync(ct);

        if (remaining)
        {
            return;
        }

        var sums = await db.PaymentUploadChunks
            .AsNoTracking()
            .Where(x => x.UploadId == uploadId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Processed = g.Sum(x => x.ProcessedRows),
                Succeeded = g.Sum(x => x.SucceededRows),
                Failed = g.Sum(x => x.FailedRows),
                AnyFailedChunk = g.Any(x => x.Status == ChunkStatus.Failed)
            })
            .FirstOrDefaultAsync(ct);

        var upload = await db.PaymentUploads.FirstOrDefaultAsync(x => x.Id == uploadId, ct);
        if (upload is null)
        {
            return;
        }

        if (upload.Status is UploadStatus.Completed or UploadStatus.Failed)
        {
            return;
        }

        upload.ProcessedRows = sums?.Processed ?? 0;
        upload.SucceededRows = sums?.Succeeded ?? 0;
        upload.FailedRows = sums?.Failed ?? 0;

        upload.Status = (sums?.AnyFailedChunk ?? false) ? UploadStatus.Failed : UploadStatus.Completed;
        upload.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // Notify clients of final status
        if (upload.Status == UploadStatus.Completed)
        {
            await notificationService.NotifyUploadCompleted(
                upload.Id, 
                upload.Token, 
                upload.TotalRows ?? 0, 
                upload.ProcessedRows, 
                upload.SucceededRows, 
                upload.FailedRows);
        }
        else
        {
            await notificationService.NotifyUploadFailed(upload.Id, upload.Token, upload.LastError ?? "One or more chunks failed");
        }
    }

    private async Task SucceedJobAsync(UploadPaymentsDbContext db, PaymentUploadJob job, CancellationToken ct)
    {
        job.Status = JobStatus.Succeeded;
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.LockedAtUtc = null;
        job.LockedBy = null;
        job.HeartbeatAtUtc = null;
        await db.SaveChangesAsync(ct);
    }

    private async Task FailJobAsync(UploadPaymentsDbContext db, PaymentUploadJob job, string error, CancellationToken ct)
    {
        job.AttemptCount++;
        job.LastError = error;
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.LockedAtUtc = null;
        job.LockedBy = null;
        job.HeartbeatAtUtc = null;

        if (job.AttemptCount >= _options.MaxAttempts)
        {
            job.Status = JobStatus.Failed;

            var upload = await db.PaymentUploads.FirstOrDefaultAsync(x => x.Id == job.UploadId, ct);
            if (upload is not null)
            {
                upload.Status = UploadStatus.Failed;
                upload.LastError = error;
                upload.UpdatedAtUtc = DateTime.UtcNow;
                
                await db.SaveChangesAsync(ct);
                
                // Notify clients of upload failure
                await notificationService.NotifyUploadFailed(upload.Id, upload.Token, error);
            }
        }
        else
        {
            job.Status = JobStatus.Queued;
            job.NextRunAtUtc = DateTime.UtcNow.AddSeconds(2 * Math.Pow(2, job.AttemptCount));
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task FailChunkAsync(UploadPaymentsDbContext db, PaymentUploadChunk chunk, string error, CancellationToken ct)
    {
        chunk.AttemptCount++;
        chunk.LastError = error;
        chunk.LockedAtUtc = null;
        chunk.LockedBy = null;
        chunk.HeartbeatAtUtc = null;
        chunk.UpdatedAtUtc = DateTime.UtcNow;

        if (chunk.AttemptCount >= _options.MaxAttempts)
        {
            chunk.Status = ChunkStatus.Failed;
        }
        else
        {
            chunk.Status = ChunkStatus.Queued;
            chunk.NextRunAtUtc = DateTime.UtcNow.AddSeconds(2 * Math.Pow(2, chunk.AttemptCount));
        }

        await db.SaveChangesAsync(ct);
    }

    private static string NormalizeHeader(string header)
    {
        header = header.Trim();
        header = header.Trim('\ufeff');
        return header;
    }

    private static string NormalizeValueForField(string? fieldName, string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        value = value.Trim();
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return value;
        }

        if (fieldName.Contains("iban", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("swift", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("accountnumber", StringComparison.OrdinalIgnoreCase))
        {
            value = value.Replace(" ", "", StringComparison.Ordinal);
        }

        return value;
    }

    private sealed record RuleFailure(Guid? RuleId, string? FieldName, string Code, string Message, RuleSeverity Severity);

    private static IReadOnlyList<RuleFailure> EvaluateRule(ValidationRule rule, string? fieldName, string? rawValue)
    {
        var failures = new List<RuleFailure>();

        var value = NormalizeValueForField(fieldName, rawValue);
        var parameters = ParseParams(rule.ParametersJson);

        switch (rule.RuleType)
        {
            case RuleType.Required:
                if (string.IsNullOrWhiteSpace(value))
                {
                    failures.Add(new RuleFailure(rule.Id, fieldName, rule.Code, ResolveMessage(rule, fieldName, "is required"), rule.Severity));
                }
                break;

            case RuleType.Regex:
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        break;
                    }

                    if (!parameters.TryGetValue("pattern", out var patternObj) || patternObj is not string pattern || string.IsNullOrWhiteSpace(pattern))
                    {
                        break;
                    }

                    var options = RegexOptions.CultureInvariant;
                    if (parameters.TryGetValue("ignoreCase", out var ignoreCaseObj) && ignoreCaseObj is bool ignoreCase && ignoreCase)
                    {
                        options |= RegexOptions.IgnoreCase;
                    }

                    if (!Regex.IsMatch(value, pattern, options))
                    {
                        failures.Add(new RuleFailure(rule.Id, fieldName, rule.Code, ResolveMessage(rule, fieldName, "has invalid format"), rule.Severity));
                    }
                }
                break;

            case RuleType.AllowedValues:
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        break;
                    }

                    if (!parameters.TryGetValue("values", out var valuesObj) || valuesObj is not JsonElement valuesElement || valuesElement.ValueKind != JsonValueKind.Array)
                    {
                        break;
                    }

                    var ignoreCase = parameters.TryGetValue("ignoreCase", out var ignoreCaseObj) && ignoreCaseObj is bool ic && ic;

                    var allowed = valuesElement.EnumerateArray()
                        .Select(x => x.GetString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => ignoreCase ? x!.Trim().ToUpperInvariant() : x!.Trim())
                        .ToHashSet(StringComparer.Ordinal);

                    var candidate = ignoreCase ? value.ToUpperInvariant() : value;
                    if (!allowed.Contains(candidate))
                    {
                        failures.Add(new RuleFailure(rule.Id, fieldName, rule.Code, ResolveMessage(rule, fieldName, "is not an allowed value"), rule.Severity));
                    }
                }
                break;

            case RuleType.DecimalRange:
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        break;
                    }

                    if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                    {
                        failures.Add(new RuleFailure(rule.Id, fieldName, rule.Code, ResolveMessage(rule, fieldName, "is not a valid decimal"), rule.Severity));
                        break;
                    }

                    var min = parameters.TryGetValue("min", out var minObj) ? Convert.ToDecimal(minObj, CultureInfo.InvariantCulture) : (decimal?)null;
                    var max = parameters.TryGetValue("max", out var maxObj) ? Convert.ToDecimal(maxObj, CultureInfo.InvariantCulture) : (decimal?)null;

                    if (min is not null && amount < min)
                    {
                        failures.Add(new RuleFailure(rule.Id, fieldName, rule.Code, ResolveMessage(rule, fieldName, $"must be >= {min}"), rule.Severity));
                    }

                    if (max is not null && amount > max)
                    {
                        failures.Add(new RuleFailure(rule.Id, fieldName, rule.Code, ResolveMessage(rule, fieldName, $"must be <= {max}"), rule.Severity));
                    }
                }
                break;

            case RuleType.DateFormat:
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        break;
                    }

                    if (!parameters.TryGetValue("format", out var formatObj) || formatObj is not string format || string.IsNullOrWhiteSpace(format))
                    {
                        break;
                    }

                    if (!DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        failures.Add(new RuleFailure(rule.Id, fieldName, rule.Code, ResolveMessage(rule, fieldName, $"must match format {format}"), rule.Severity));
                    }
                }
                break;
        }

        return failures;
    }

    private static Dictionary<string, object?> ParseParams(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetDecimal(out var d) ? d : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Array => prop.Value,
                    _ => prop.Value.ToString()
                };
            }
            return dict;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string ResolveMessage(ValidationRule rule, string? fieldName, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(rule.MessageTemplate))
        {
            return rule.MessageTemplate
                .Replace("{FieldName}", fieldName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return string.IsNullOrWhiteSpace(fieldName) ? fallback : $"{fieldName} {fallback}";
    }
}
