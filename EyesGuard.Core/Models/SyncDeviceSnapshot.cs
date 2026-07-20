namespace EyesGuard.Core.Models;

public sealed record SyncDeviceSnapshot
{
    public string DeviceId { get; init; } = string.Empty;
    public long Revision { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public BreakSettings Settings { get; init; } = new();
    public IReadOnlyList<SyncUsageDay> Usage { get; init; } = [];

    public SyncDeviceSnapshot Normalize() => this with
    {
        DeviceId = string.IsNullOrWhiteSpace(DeviceId) ? "unknown-device" : DeviceId.Trim(),
        Revision = Math.Max(0, Revision),
        Settings = (Settings ?? new BreakSettings()).Normalize(),
        Usage = (Usage ?? []).Select(day => day.Normalize(DeviceId)).Where(day => !string.IsNullOrWhiteSpace(day.DeviceId)).ToArray()
    };
}
