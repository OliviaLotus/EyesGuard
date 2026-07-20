namespace EyesGuard.Core.Models;

public sealed record SyncDocument
{
    public IReadOnlyList<SyncDeviceSnapshot> Devices { get; init; } = [];

    public SyncDocument Normalize()
    {
        var devices = (Devices ?? [])
            .Select(snapshot => snapshot.Normalize())
            .GroupBy(snapshot => snapshot.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(snapshot => snapshot.Revision)
                .ThenByDescending(snapshot => snapshot.UpdatedAt)
                .First())
            .OrderBy(snapshot => snapshot.DeviceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return this with { Devices = devices };
    }

    public SyncDeviceSnapshot? GetDevice(string deviceId) =>
        Normalize().Devices.FirstOrDefault(snapshot =>
            string.Equals(snapshot.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

    public BreakSettings? GetLatestSettings()
    {
        return Normalize().Devices
            .OrderByDescending(snapshot => snapshot.UpdatedAt)
            .ThenByDescending(snapshot => snapshot.Revision)
            .ThenBy(snapshot => snapshot.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Select(snapshot => snapshot.Settings)
            .FirstOrDefault();
    }
}
