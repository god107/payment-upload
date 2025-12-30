namespace UploadPayments.Worker;

public sealed class ValidationWorkerOptions
{
    public int PollDelayMilliseconds { get; set; } = 500;

    public int ChunkSizeRows { get; set; } = 1000;

    public int HeartbeatSeconds { get; set; } = 10;

    public int StaleLockSeconds { get; set; } = 60;

    public int MaxAttempts { get; set; } = 5;
}
