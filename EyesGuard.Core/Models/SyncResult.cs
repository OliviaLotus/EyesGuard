namespace EyesGuard.Core.Models;

public sealed record SyncResult(
    SyncStatus Status,
    int PendingCount,
    string Message,
    DateTimeOffset? CompletedAt = null);
