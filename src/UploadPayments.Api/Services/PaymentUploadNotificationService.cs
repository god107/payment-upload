using Microsoft.AspNetCore.SignalR;
using UploadPayments.Api.Hubs;
using UploadPayments.Contracts;

namespace UploadPayments.Api.Services;

/// <summary>
/// Service for sending SignalR notifications about payment upload progress.
/// This service is injected into the Worker to enable real-time updates to connected clients.
/// </summary>
public sealed class PaymentUploadNotificationService(IHubContext<PaymentUploadHub, IPaymentUploadClient> hubContext) 
    : IPaymentUploadNotificationService
{
    /// <summary>
    /// Notify clients when upload status changes.
    /// </summary>
    public async Task NotifyUploadStatusChanged(Guid uploadId, Guid token, string status, int? totalRows = null, int? totalChunks = null)
    {
        await hubContext.Clients
            .Group($"upload_{token}")
            .UploadStatusChanged(uploadId, token, status, totalRows, totalChunks);
    }

    /// <summary>
    /// Notify clients when a validation chunk completes.
    /// </summary>
    public async Task NotifyChunkCompleted(Guid uploadId, Guid token, int chunkIndex, int processedRows, int succeededRows, int failedRows, int totalChunks, int completedChunks)
    {
        await hubContext.Clients
            .Group($"upload_{token}")
            .ChunkCompleted(uploadId, token, chunkIndex, processedRows, succeededRows, failedRows, totalChunks, completedChunks);
    }

    /// <summary>
    /// Notify clients when the entire upload finishes successfully.
    /// </summary>
    public async Task NotifyUploadCompleted(Guid uploadId, Guid token, int totalRows, int processedRows, int succeededRows, int failedRows)
    {
        await hubContext.Clients
            .Group($"upload_{token}")
            .UploadCompleted(uploadId, token, totalRows, processedRows, succeededRows, failedRows);
    }

    /// <summary>
    /// Notify clients when an upload fails.
    /// </summary>
    public async Task NotifyUploadFailed(Guid uploadId, Guid token, string error)
    {
        await hubContext.Clients
            .Group($"upload_{token}")
            .UploadFailed(uploadId, token, error);
    }

    /// <summary>
    /// Notify clients when a chunk fails.
    /// </summary>
    public async Task NotifyChunkFailed(Guid uploadId, Guid token, int chunkIndex, string error, int attemptCount, int maxAttempts)
    {
        await hubContext.Clients
            .Group($"upload_{token}")
            .ChunkFailed(uploadId, token, chunkIndex, error, attemptCount, maxAttempts);
    }

    /// <summary>
    /// Notify clients of row-level progress (use sparingly).
    /// </summary>
    public async Task NotifyRowProgressUpdate(Guid uploadId, Guid token, int chunkIndex, int processedInChunk, int totalInChunk)
    {
        await hubContext.Clients
            .Group($"upload_{token}")
            .RowProgressUpdate(uploadId, token, chunkIndex, processedInChunk, totalInChunk);
    }

    /// <summary>
    /// Notify clients when an upload has been deleted.
    /// </summary>
    public async Task NotifyUploadDeleted(Guid uploadId, Guid token)
    {
        await hubContext.Clients
            .Group($"upload_{token}")
            .UploadDeleted(uploadId, token);
    }
}
