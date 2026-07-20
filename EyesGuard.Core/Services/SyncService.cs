using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.Core.Services;

public sealed class SyncService
{
    private readonly ISyncStateStore _stateStore;
    private readonly ISyncTransport _transport;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SyncService(string deviceId, ISyncStateStore stateStore, ISyncTransport transport, IClock clock)
    {
        DeviceId = string.IsNullOrWhiteSpace(deviceId) ? throw new ArgumentException("Device id is required.", nameof(deviceId)) : deviceId.Trim();
        _stateStore = stateStore;
        _transport = transport;
        _clock = clock;
    }

    public string DeviceId { get; }
    public SyncSettingsView SettingsView { get; private set; } = SyncSettingsView.Empty;

    public async Task<SyncResult> QueueSnapshotAsync(
        BreakSettings settings,
        IReadOnlyList<UsageSummary> usage,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = (await _stateStore.LoadAsync(cancellationToken)).Normalize();
            var current = state.Document.GetDevice(DeviceId);
            var revision = (current?.Revision ?? 0) + 1;
            var now = _clock.Now;
            var snapshot = new SyncDeviceSnapshot
            {
                DeviceId = DeviceId,
                Revision = revision,
                UpdatedAt = now,
                Settings = settings.Normalize(),
                Usage = usage.Select(summary => new SyncUsageDay
                {
                    DeviceId = DeviceId,
                    Day = summary.Day,
                    ActiveTime = summary.ActiveTime,
                    LongestSession = summary.LongestSession,
                    NightTime = summary.NightTime,
                    BreaksCompleted = summary.BreaksCompleted,
                    BreaksSkipped = summary.BreaksSkipped,
                    BreaksSnoozed = summary.BreaksSnoozed
                }).ToArray()
            }.Normalize();

            var document = new SyncDocument
            {
                Devices = state.Document.Devices
                    .Where(item => !string.Equals(item.DeviceId, DeviceId, StringComparison.OrdinalIgnoreCase))
                    .Append(snapshot)
                    .ToArray()
            }.Normalize();
            var outbox = state.Outbox
                .Where(item => !string.Equals(item.DeviceId, DeviceId, StringComparison.OrdinalIgnoreCase))
                .Append(new SyncOutboxItem { DeviceId = DeviceId, Revision = revision, QueuedAt = now })
                .ToArray();
            state = state with { DeviceId = DeviceId, Document = document, Outbox = outbox, LastError = null };
            await _stateStore.SaveAsync(state, cancellationToken);
            return await TrySyncCoreAsync(state, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SyncResult> TrySyncAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = (await _stateStore.LoadAsync(cancellationToken)).Normalize();
            return await TrySyncCoreAsync(state, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SyncResult> TrySyncCoreAsync(SyncState state, CancellationToken cancellationToken)
    {
        SettingsView = BuildSettingsView(state);
        if (state.Outbox.Count == 0)
        {
            return new SyncResult(SyncStatus.Succeeded, 0, "没有待同步内容", state.LastSuccessfulSyncAt);
        }

        if (!_transport.IsConfigured)
        {
            return new SyncResult(SyncStatus.NotConfigured, state.Outbox.Count,
                "尚未配置同步服务器，内容已保存在本地待同步队列中");
        }

        try
        {
            var remote = await _transport.PullAsync(cancellationToken);
            var merged = SyncMerger.Merge(state.Document, remote);
            await _transport.PushAsync(merged, cancellationToken);
            var synced = state with
            {
                Document = merged,
                Outbox = [],
                LastSuccessfulSyncAt = _clock.Now,
                LastError = null
            };
            await _stateStore.SaveAsync(synced, cancellationToken);
            SettingsView = BuildSettingsView(synced);
            return new SyncResult(SyncStatus.Succeeded, 0, "同步完成", synced.LastSuccessfulSyncAt);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var failed = state with { LastError = exception.Message, Outbox = state.Outbox.Select(item => item with
            {
                AttemptCount = item.AttemptCount + 1,
                LastError = exception.Message
            }).ToArray() };
            await _stateStore.SaveAsync(failed, cancellationToken);
            SettingsView = BuildSettingsView(failed);
            return new SyncResult(SyncStatus.Failed, failed.Outbox.Count, $"同步失败：{exception.Message}");
        }
    }

    private SyncSettingsView BuildSettingsView(SyncState state) => new(
        state.Document.GetLatestSettings() ?? new BreakSettings(),
        state.Outbox.Count,
        state.LastSuccessfulSyncAt,
        state.LastError);
}

public sealed record SyncSettingsView(
    BreakSettings Settings,
    int PendingCount,
    DateTimeOffset? LastSuccessfulSyncAt,
    string? LastError)
{
    public static SyncSettingsView Empty { get; } = new(new BreakSettings(), 0, null, null);
}
