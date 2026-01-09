using UploadPayments.Contracts;
using System.Net.Http.Json;

namespace UploadPayments.Worker.Services;

/// <summary>
/// HTTP-based notification client that calls the API's notification endpoint.
/// This allows the Worker (separate service) to trigger SignalR notifications via API.
/// </summary>
public sealed class HttpPaymentUploadNotificationService(HttpClient httpClient, ILogger<HttpPaymentUploadNotificationService> logger) 
    : IPaymentUploadNotificationService
{
    public async Task NotifyUploadStatusChanged(Guid uploadId, Guid token, string status, int? totalRows = null, int? totalChunks = null)
    {
        try
        {
            var payload = new { uploadId, token, status, totalRows, totalChunks };
            await httpClient.PostAsJsonAsync("/api/notifications/upload-status-changed", payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send upload status notification for {UploadId}", uploadId);
        }
    }

    public async Task NotifyChunkCompleted(Guid uploadId, Guid token, int chunkIndex, int processedRows, int succeededRows, int failedRows, int totalChunks, int completedChunks)
    {
        try
        {
            var payload = new { uploadId, token, chunkIndex, processedRows, succeededRows, failedRows, totalChunks, completedChunks };
            await httpClient.PostAsJsonAsync("/api/notifications/chunk-completed", payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send chunk completed notification for {UploadId} chunk {ChunkIndex}", uploadId, chunkIndex);
        }
    }

    public async Task NotifyUploadCompleted(Guid uploadId, Guid token, int totalRows, int processedRows, int succeededRows, int failedRows)
    {
        try
        {
            var payload = new { uploadId, token, totalRows, processedRows, succeededRows, failedRows };
            await httpClient.PostAsJsonAsync("/api/notifications/upload-completed", payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send upload completed notification for {UploadId}", uploadId);
        }
    }

    public async Task NotifyUploadFailed(Guid uploadId, Guid token, string error)
    {
        try
        {
            var payload = new { uploadId, token, error };
            await httpClient.PostAsJsonAsync("/api/notifications/upload-failed", payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send upload failed notification for {UploadId}", uploadId);
        }
    }

    public async Task NotifyChunkFailed(Guid uploadId, Guid token, int chunkIndex, string error, int attemptCount, int maxAttempts)
    {
        try
        {
            var payload = new { uploadId, token, chunkIndex, error, attemptCount, maxAttempts };
            await httpClient.PostAsJsonAsync("/api/notifications/chunk-failed", payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send chunk failed notification for {UploadId} chunk {ChunkIndex}", uploadId, chunkIndex);
        }
    }

    public async Task NotifyRowProgressUpdate(Guid uploadId, Guid token, int chunkIndex, int processedInChunk, int totalInChunk)
    {
        try
        {
            var payload = new { uploadId, token, chunkIndex, processedInChunk, totalInChunk };
            await httpClient.PostAsJsonAsync("/api/notifications/row-progress", payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send row progress notification for {UploadId} chunk {ChunkIndex}", uploadId, chunkIndex);
        }
    }
}
