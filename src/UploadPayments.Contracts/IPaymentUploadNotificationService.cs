namespace UploadPayments.Contracts;

/// <summary>
/// Interface for sending payment upload notifications.
/// Implementations can use HTTP, SignalR, message queues, etc.
/// </summary>
public interface IPaymentUploadNotificationService
{
    Task NotifyUploadStatusChanged(Guid uploadId, Guid token, string status, int? totalRows = null, int? totalChunks = null);
    Task NotifyChunkCompleted(Guid uploadId, Guid token, int chunkIndex, int processedRows, int succeededRows, int failedRows, int totalChunks, int completedChunks);
    Task NotifyUploadCompleted(Guid uploadId, Guid token, int totalRows, int processedRows, int succeededRows, int failedRows);
    Task NotifyUploadFailed(Guid uploadId, Guid token, string error);
    Task NotifyChunkFailed(Guid uploadId, Guid token, int chunkIndex, string error, int attemptCount, int maxAttempts);
    Task NotifyRowProgressUpdate(Guid uploadId, Guid token, int chunkIndex, int processedInChunk, int totalInChunk);
    Task NotifyUploadDeleted(Guid uploadId, Guid token);
}
