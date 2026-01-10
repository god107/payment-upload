using UploadPayments.Contracts;

namespace UploadPayments.Api.Hubs;

/// <summary>
/// Strongly-typed SignalR client interface for upload notifications.
/// These methods are called on connected clients to push real-time updates.
/// </summary>
public interface IPaymentUploadClient
{
    /// <summary>
    /// Notifies clients when upload status changes (Queued → Parsing → Validating → Completed/Failed).
    /// </summary>
    Task UploadStatusChanged(Guid uploadId, Guid token, string status, int? totalRows, int? totalChunks);

    /// <summary>
    /// Notifies clients when a validation chunk completes.
    /// </summary>
    Task ChunkCompleted(Guid uploadId, Guid token, int chunkIndex, int processedRows, int succeededRows, int failedRows, int totalChunks, int completedChunks);

    /// <summary>
    /// Notifies clients when the entire upload finishes successfully.
    /// </summary>
    Task UploadCompleted(Guid uploadId, Guid token, int totalRows, int processedRows, int succeededRows, int failedRows);

    /// <summary>
    /// Notifies clients when an upload fails.
    /// </summary>
    Task UploadFailed(Guid uploadId, Guid token, string error);

    /// <summary>
    /// Notifies clients when a chunk fails (for retry tracking).
    /// </summary>
    Task ChunkFailed(Guid uploadId, Guid token, int chunkIndex, string error, int attemptCount, int maxAttempts);

    /// <summary>
    /// Optional: Fine-grained row progress updates within a chunk.
    /// Use sparingly to avoid overwhelming clients.
    /// </summary>
    Task RowProgressUpdate(Guid uploadId, Guid token, int chunkIndex, int processedInChunk, int totalInChunk);

    /// <summary>
    /// Notifies clients when an upload has been deleted.
    /// </summary>
    Task UploadDeleted(Guid uploadId, Guid token);
}
