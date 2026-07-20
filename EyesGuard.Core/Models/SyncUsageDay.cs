namespace EyesGuard.Core.Models;

public sealed record SyncUsageDay
{
    public string DeviceId { get; init; } = string.Empty;
    public DateOnly Day { get; init; }
    public TimeSpan ActiveTime { get; init; }
    public TimeSpan LongestSession { get; init; }
    public TimeSpan NightTime { get; init; }
    public int BreaksCompleted { get; init; }
    public int BreaksSkipped { get; init; }
    public int BreaksSnoozed { get; init; }

    public SyncUsageDay Normalize(string? fallbackDeviceId = null) => this with
    {
        DeviceId = string.IsNullOrWhiteSpace(DeviceId) ? fallbackDeviceId?.Trim() ?? string.Empty : DeviceId.Trim(),
        ActiveTime = NonNegative(ActiveTime),
        LongestSession = NonNegative(LongestSession),
        NightTime = NonNegative(NightTime),
        BreaksCompleted = Math.Max(0, BreaksCompleted),
        BreaksSkipped = Math.Max(0, BreaksSkipped),
        BreaksSnoozed = Math.Max(0, BreaksSnoozed)
    };

    public UsageSummary ToUsageSummary() => new(
        Day,
        ActiveTime,
        LongestSession,
        NightTime,
        BreaksCompleted,
        BreaksSkipped,
        BreaksSnoozed);

    private static TimeSpan NonNegative(TimeSpan value) => value < TimeSpan.Zero ? TimeSpan.Zero : value;
}
