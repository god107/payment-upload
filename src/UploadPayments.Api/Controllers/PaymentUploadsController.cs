using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using UploadPayments.Api.Contracts;
using UploadPayments.Infrastructure.Persistence;
using UploadPayments.Infrastructure.Persistence.Entities;

namespace UploadPayments.Api.Controllers;

[ApiController]
[Route("api/payment-uploads")]
public sealed class PaymentUploadsController(UploadPaymentsDbContext db) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UploadAcceptedDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only .csv files are supported.");
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var now = DateTime.UtcNow;

        var upload = new PaymentUpload
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid(),
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            ContentSha256 = sha,
            Status = UploadStatus.Queued,
            RawCsvBytes = bytes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var job = new PaymentUploadJob
        {
            Id = Guid.NewGuid(),
            UploadId = upload.Id,
            JobType = JobType.ParseCsv,
            Status = JobStatus.Queued,
            AttemptCount = 0,
            NextRunAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.PaymentUploads.Add(upload);
        db.PaymentUploadJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        var statusUrl = Url.ActionLink(nameof(GetStatus), values: new { uploadId = upload.Id, token = upload.Token }) ?? string.Empty;
        var errorsUrl = Url.ActionLink(nameof(GetErrors), values: new { uploadId = upload.Id, token = upload.Token }) ?? string.Empty;

        return Accepted(new UploadAcceptedDto(upload.Id, upload.Token, statusUrl, errorsUrl));
    }

    [HttpGet("{uploadId:guid}/status")]
    [ProducesResponseType(typeof(UploadStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus([FromRoute] Guid uploadId, [FromQuery] Guid token, CancellationToken cancellationToken)
    {
        var upload = await db.PaymentUploads
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == uploadId && x.Token == token, cancellationToken);

        if (upload is null)
        {
            return NotFound();
        }

        var chunks = await db.PaymentUploadChunks
            .AsNoTracking()
            .Where(x => x.UploadId == uploadId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Succeeded = g.Count(x => x.Status == ChunkStatus.Succeeded),
                Failed = g.Count(x => x.Status == ChunkStatus.Failed),
                Running = g.Count(x => x.Status == ChunkStatus.Running),
                Queued = g.Count(x => x.Status == ChunkStatus.Queued),
                ProcessedRows = g.Sum(x => x.ProcessedRows),
                SucceededRows = g.Sum(x => x.SucceededRows),
                FailedRows = g.Sum(x => x.FailedRows)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var dto = new UploadStatusDto(
            upload.Id,
            upload.Status.ToString(),
            upload.CreatedAtUtc,
            upload.UpdatedAtUtc,
            upload.TotalRows,
            chunks?.ProcessedRows ?? upload.ProcessedRows,
            chunks?.SucceededRows ?? upload.SucceededRows,
            chunks?.FailedRows ?? upload.FailedRows,
            chunks?.Total ?? 0,
            chunks?.Succeeded ?? 0,
            chunks?.Failed ?? 0,
            chunks?.Running ?? 0,
            chunks?.Queued ?? 0,
            upload.LastError);

        return Ok(dto);
    }

    [HttpGet("{uploadId:guid}/errors")]
    [ProducesResponseType(typeof(UploadErrorsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetErrors(
        [FromRoute] Guid uploadId,
        [FromQuery] Guid token,
        [FromQuery] int? cursorRow,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 2000)
        {
            return BadRequest("limit must be between 1 and 2000.");
        }

        var uploadExists = await db.PaymentUploads
            .AsNoTracking()
            .AnyAsync(x => x.Id == uploadId && x.Token == token, cancellationToken);

        if (!uploadExists)
        {
            return NotFound();
        }

        var startRow = cursorRow.GetValueOrDefault(0);

        var errors = await db.PaymentUploadRowErrors
            .AsNoTracking()
            .Where(x => x.UploadId == uploadId && x.IsError)
            .Where(x => x.RowNumber > startRow)
            .OrderBy(x => x.RowNumber)
            .ThenBy(x => x.Code)
            .Take(limit)
            .Select(x => new UploadRowErrorDto(
                x.RowNumber,
                x.FieldName,
                x.Code,
                x.Message,
                x.Severity.ToString()))
            .ToListAsync(cancellationToken);

        int? nextCursor = errors.Count == limit ? errors[^1].RowNumber : null;

        var totalRows = await db.PaymentUploads
            .AsNoTracking()
            .Where(x => x.Id == uploadId)
            .Select(x => x.TotalRows)
            .FirstOrDefaultAsync(cancellationToken);

        var dto = new UploadErrorsPageDto(uploadId, totalRows, errors.Count, nextCursor, errors);
        return Ok(dto);
    }
}
