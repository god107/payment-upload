using Microsoft.AspNetCore.Mvc;
using UploadPayments.Contracts;

namespace UploadPayments.Api.Controllers;

/// <summary>
/// Internal notification endpoint for Worker to trigger SignalR notifications.
/// This controller receives HTTP calls from Worker and broadcasts via SignalR.
/// </summary>
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(IPaymentUploadNotificationService notificationService) : ControllerBase
{
    [HttpPost("upload-status-changed")]
    public async Task<IActionResult> UploadStatusChanged([FromBody] UploadStatusChangedRequest request)
    {
        await notificationService.NotifyUploadStatusChanged(
            request.UploadId, 
            request.Token, 
            request.Status, 
            request.TotalRows, 
            request.TotalChunks);
        return Ok();
    }

    [HttpPost("chunk-completed")]
    public async Task<IActionResult> ChunkCompleted([FromBody] ChunkCompletedRequest request)
    {
        await notificationService.NotifyChunkCompleted(
            request.UploadId,
            request.Token,
            request.ChunkIndex,
            request.ProcessedRows,
            request.SucceededRows,
            request.FailedRows,
            request.TotalChunks,
            request.CompletedChunks);
        return Ok();
    }

    [HttpPost("upload-completed")]
    public async Task<IActionResult> UploadCompleted([FromBody] UploadCompletedRequest request)
    {
        await notificationService.NotifyUploadCompleted(
            request.UploadId,
            request.Token,
            request.TotalRows,
            request.ProcessedRows,
            request.SucceededRows,
            request.FailedRows);
        return Ok();
    }

    [HttpPost("upload-failed")]
    public async Task<IActionResult> UploadFailed([FromBody] UploadFailedRequest request)
    {
        await notificationService.NotifyUploadFailed(request.UploadId, request.Token, request.Error);
        return Ok();
    }

    [HttpPost("chunk-failed")]
    public async Task<IActionResult> ChunkFailed([FromBody] ChunkFailedRequest request)
    {
        await notificationService.NotifyChunkFailed(
            request.UploadId,
            request.Token,
            request.ChunkIndex,
            request.Error,
            request.AttemptCount,
            request.MaxAttempts);
        return Ok();
    }

    [HttpPost("row-progress")]
    public async Task<IActionResult> RowProgress([FromBody] RowProgressRequest request)
    {
        await notificationService.NotifyRowProgressUpdate(
            request.UploadId,
            request.Token,
            request.ChunkIndex,
            request.ProcessedInChunk,
            request.TotalInChunk);
        return Ok();
    }
}

// Request DTOs
public sealed record UploadStatusChangedRequest(Guid UploadId, Guid Token, string Status, int? TotalRows, int? TotalChunks);
public sealed record ChunkCompletedRequest(Guid UploadId, Guid Token, int ChunkIndex, int ProcessedRows, int SucceededRows, int FailedRows, int TotalChunks, int CompletedChunks);
public sealed record UploadCompletedRequest(Guid UploadId, Guid Token, int TotalRows, int ProcessedRows, int SucceededRows, int FailedRows);
public sealed record UploadFailedRequest(Guid UploadId, Guid Token, string Error);
public sealed record ChunkFailedRequest(Guid UploadId, Guid Token, int ChunkIndex, string Error, int AttemptCount, int MaxAttempts);
public sealed record RowProgressRequest(Guid UploadId, Guid Token, int ChunkIndex, int ProcessedInChunk, int TotalInChunk);
