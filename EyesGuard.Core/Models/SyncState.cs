namespace EyesGuard.Core.Models;

public sealed record SyncState
{
    public string DeviceId { get; init; } = string.Empty;
    public SyncDocument Document { get; init; } = new();
    public IReadOnlyList<SyncOutboxItem> Outbox { get; init; } = [];
    public DateTimeOffset? LastSuccessfulSyncAt { get; init; }
    public string? LastError { get; init; }

    public SyncState Normalize() => this with
    {
        DeviceId = string.IsNullOrWhiteSpace(DeviceId) ? "unknown-device" : DeviceId.Trim(),
        Document = (Document ?? new SyncDocument()).Normalize(),
        Outbox = (Outbox ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId) && item.Revision >= 0)
            .GroupBy(item => $"{item.DeviceId}:{item.Revision}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.AttemptCount).First())
            .OrderBy(item => item.QueuedAt)
            .ToArray()
    };
}
