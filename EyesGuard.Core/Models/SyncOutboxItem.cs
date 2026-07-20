namespace EyesGuard.Core.Models;

public sealed record SyncOutboxItem
{
    public string DeviceId { get; init; } = string.Empty;
    public long Revision { get; init; }
    public DateTimeOffset QueuedAt { get; init; }
    public int AttemptCount { get; init; }
    public string? LastError { get; init; }
}
