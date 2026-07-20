using EyesGuard.Core.Models;

namespace EyesGuard.Core.Services;

public static class SyncMerger
{
    public static SyncDocument Merge(SyncDocument? local, SyncDocument? remote)
    {
        var snapshots = (local?.Normalize().Devices ?? [])
            .Concat(remote?.Normalize().Devices ?? [])
            .GroupBy(snapshot => snapshot.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(snapshot => snapshot.Revision)
                .ThenByDescending(snapshot => snapshot.UpdatedAt)
                .ThenBy(snapshot => snapshot.DeviceId, StringComparer.OrdinalIgnoreCase)
                .First())
            .ToArray();

        return new SyncDocument { Devices = snapshots }.Normalize();
    }

    public static IReadOnlyList<UsageSummary> AggregateUsage(SyncDocument document)
    {
        var usage = document.Normalize().Devices
            .SelectMany(snapshot => snapshot.Usage.Select(day => day.Normalize(snapshot.DeviceId)))
            .GroupBy(day => day.Day)
            .OrderBy(group => group.Key)
            .Select(group => new UsageSummary(
                group.Key,
                TimeSpan.FromTicks(group.Sum(day => day.ActiveTime.Ticks)),
                TimeSpan.FromTicks(group.Max(day => day.LongestSession.Ticks)),
                TimeSpan.FromTicks(group.Sum(day => day.NightTime.Ticks)),
                group.Sum(day => day.BreaksCompleted),
                group.Sum(day => day.BreaksSkipped),
                group.Sum(day => day.BreaksSnoozed)))
            .ToArray();
        return usage;
    }
}
