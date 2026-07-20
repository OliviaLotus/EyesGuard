using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.Core.Services;

/// <summary>
/// 休息提醒服务 - 整合状态机、持久化和事件发布
/// </summary>
public sealed class BreakReminderService : IAsyncDisposable
{
    private readonly IClock _clock;
    private readonly IIdleTimeProvider _idleTimeProvider;
    private readonly IActivityRepository _repository;
    private readonly ISettingsStore _settingsStore;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly object _bufferGate = new();

    // State machine fields
    private BreakPhase _phase = BreakPhase.Working;
    private TimeSpan _sessionElapsed = TimeSpan.Zero;
    private TimeSpan _nextBreakIn = TimeSpan.Zero;
    private TimeSpan _restRemaining = TimeSpan.Zero;
    private DateTimeOffset? _lastTick;
    private DateTimeOffset? _lastBreakDue;
    private int _breakCount;
    private int _snoozedCount;
    private int _skippedCount;

    // Usage buffering
    private TimeSpan _pendingActive;
    private TimeSpan _pendingLongestSession;
    private bool _pendingNightUsage;
    private int _flushCounter;

    private BreakSettings _settings;
    private Task? _runtimeTask;

    public BreakReminderService(
        BreakSettings settings,
        IClock clock,
        IIdleTimeProvider idleTimeProvider,
        IActivityRepository repository,
        ISettingsStore settingsStore)
    {
        _settings = settings;
        _clock = clock;
        _idleTimeProvider = idleTimeProvider;
        _repository = repository;
        _settingsStore = settingsStore;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _nextBreakIn = TimeSpan.FromMinutes(_settings.WorkMinutes);
    }

    public BreakSettings Settings => _settings;

    public event EventHandler<EyeGuardSnapshot>? SnapshotChanged;
    public event EventHandler? BreakDue;
    public event EventHandler? SummaryChanged;

    public void Start()
    {
        if (_runtimeTask is not null)
        {
            return;
        }

        _runtimeTask = Task.Run(async () =>
        {
            try
            {
                await RunAsync(_cancellation.Token);
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
            {
                // Expected on shutdown
            }
        });
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            var now = _clock.Now;
            var idleTime = _idleTimeProvider.GetIdleTime();
            var snapshot = Tick(now, idleTime);

            BufferUsage(snapshot, now);
            SnapshotChanged?.Invoke(this, snapshot);

            if (++_flushCounter >= 30)
            {
                _flushCounter = 0;
                await FlushUsageAsync(cancellationToken);
            }
        }
    }

    public EyeGuardSnapshot Tick(DateTimeOffset now, TimeSpan idleTime)
    {
        var delta = _lastTick is null ? TimeSpan.Zero : now - _lastTick.Value;
        _lastTick = now;

        if (!_settings.IsEnabled || _phase == BreakPhase.Paused)
        {
            return CreateSnapshot();
        }

        // Check night restriction
        if (IsNightRestricted(now))
        {
            if (_phase != BreakPhase.NightRestricted)
            {
                _phase = BreakPhase.NightRestricted;
                _sessionElapsed = TimeSpan.Zero;
            }
            return CreateSnapshot();
        }
        else if (_phase == BreakPhase.NightRestricted)
        {
            _phase = BreakPhase.Working;
            _nextBreakIn = TimeSpan.FromMinutes(_settings.WorkMinutes);
        }

        // Check do-not-disturb
        if (IsDoNotDisturbActive(now) && _phase == BreakPhase.Prompt)
        {
            _nextBreakIn = TimeSpan.FromMinutes(_settings.SnoozeMinutes);
            _phase = BreakPhase.Working;
            return CreateSnapshot();
        }

        // Check idle reset
        if (idleTime >= TimeSpan.FromMinutes(_settings.IdleResetMinutes))
        {
            if (_phase != BreakPhase.Idle && _phase != BreakPhase.Resting)
            {
                _phase = BreakPhase.Idle;
                _sessionElapsed = TimeSpan.Zero;
            }
            return CreateSnapshot();
        }
        else if (_phase == BreakPhase.Idle)
        {
            _phase = BreakPhase.Working;
            _nextBreakIn = TimeSpan.FromMinutes(_settings.WorkMinutes);
        }

        // Handle resting phase
        if (_phase == BreakPhase.Resting)
        {
            _restRemaining -= delta;
            if (_restRemaining <= TimeSpan.Zero)
            {
                CompleteBreak(now);
            }
            return CreateSnapshot();
        }

        // Handle working/prompt phase
        _sessionElapsed += delta;
        _nextBreakIn -= delta;

        if (_nextBreakIn <= TimeSpan.Zero && _phase == BreakPhase.Working)
        {
            _phase = BreakPhase.Prompt;
            if (_lastBreakDue is null || (now - _lastBreakDue.Value) > TimeSpan.FromSeconds(5))
            {
                _lastBreakDue = now;
                BreakDue?.Invoke(this, EventArgs.Empty);
            }
        }

        // Check strict mode max continuous work
        if (_settings.StrictModeEnabled &&
            _sessionElapsed >= TimeSpan.FromMinutes(_settings.MaxContinuousWorkMinutes) &&
            _phase == BreakPhase.Prompt)
        {
            return StartBreak();
        }

        return CreateSnapshot();
    }

    public EyeGuardSnapshot StartBreak()
    {
        _phase = BreakPhase.Resting;
        _restRemaining = TimeSpan.FromSeconds(_settings.BreakSeconds);
        _lastBreakDue = null;
        return CreateSnapshot();
    }

    public EyeGuardSnapshot Snooze()
    {
        if (_phase != BreakPhase.Prompt)
        {
            return CreateSnapshot();
        }

        _snoozedCount++;
        _phase = BreakPhase.Working;
        _nextBreakIn = TimeSpan.FromMinutes(_settings.SnoozeMinutes);
        return CreateSnapshot();
    }

    public EyeGuardSnapshot Skip()
    {
        if (_phase != BreakPhase.Prompt)
        {
            return CreateSnapshot();
        }

        _skippedCount++;
        _phase = BreakPhase.Working;
        _sessionElapsed = TimeSpan.Zero;
        _nextBreakIn = TimeSpan.FromMinutes(_settings.WorkMinutes);
        return CreateSnapshot();
    }

    private void CompleteBreak(DateTimeOffset now)
    {
        _breakCount++;
        _phase = BreakPhase.Working;
        _sessionElapsed = TimeSpan.Zero;
        _nextBreakIn = TimeSpan.FromMinutes(_settings.WorkMinutes);
    }

    private bool IsNightRestricted(DateTimeOffset now)
    {
        if (!_settings.NightRestrictionEnabled)
        {
            return false;
        }

        var currentTime = now.TimeOfDay;
        var start = _settings.NightRestrictionStart.ToTimeSpan();
        var end = _settings.NightRestrictionEnd.ToTimeSpan();

        return start <= end
            ? currentTime >= start && currentTime < end
            : currentTime >= start || currentTime < end;
    }

    private bool IsDoNotDisturbActive(DateTimeOffset now)
    {
        if (!_settings.DoNotDisturbEnabled)
        {
            return false;
        }

        var currentTime = now.TimeOfDay;
        var start = _settings.DoNotDisturbStart.ToTimeSpan();
        var end = _settings.DoNotDisturbEnd.ToTimeSpan();

        return start <= end
            ? currentTime >= start && currentTime < end
            : currentTime >= start || currentTime < end;
    }

    private EyeGuardSnapshot CreateSnapshot() => new(
        _phase,
        _sessionElapsed,
        _nextBreakIn,
        _restRemaining,
        TimeSpan.Zero,
        CalculateWorkProgress(),
        _settings.IsEnabled,
        _clock.Now
    );

    private double CalculateWorkProgress()
    {
        var target = TimeSpan.FromMinutes(_settings.WorkMinutes);
        var elapsed = target - _nextBreakIn;
        return target.TotalSeconds > 0 ? Math.Clamp(elapsed.TotalSeconds / target.TotalSeconds, 0, 1) : 0;
    }

    private void BufferUsage(EyeGuardSnapshot snapshot, DateTimeOffset now)
    {
        if (_phase != BreakPhase.Working && _phase != BreakPhase.Prompt)
        {
            return;
        }

        var delta = snapshot.ActiveDelta;
        if (delta <= TimeSpan.Zero)
        {
            return;
        }

        lock (_bufferGate)
        {
            _pendingActive += delta;
            _pendingLongestSession = _sessionElapsed > _pendingLongestSession
                ? _sessionElapsed
                : _pendingLongestSession;
            _pendingNightUsage |= now.Hour >= 22 || now.Hour < 7;
        }
    }

    private async Task FlushUsageAsync(CancellationToken cancellationToken)
    {
        TimeSpan active;
        TimeSpan longest;
        bool night;

        lock (_bufferGate)
        {
            active = _pendingActive;
            longest = _pendingLongestSession;
            night = _pendingNightUsage;
            _pendingActive = TimeSpan.Zero;
            _pendingLongestSession = TimeSpan.Zero;
            _pendingNightUsage = false;
        }

        if (active <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await _repository.AddUsageAsync(
                DateOnly.FromDateTime(_clock.Now.LocalDateTime),
                active,
                longest,
                night,
                cancellationToken);
            SummaryChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Re-buffer on failure
            lock (_bufferGate)
            {
                _pendingActive += active;
                if (longest > _pendingLongestSession)
                {
                    _pendingLongestSession = longest;
                }
                _pendingNightUsage |= night;
            }
        }
    }

    public Task<UsageSummary> GetTodaySummaryAsync(CancellationToken cancellationToken = default) =>
        _repository.GetSummaryAsync(DateOnly.FromDateTime(_clock.Now.LocalDateTime), cancellationToken);

    public Task<IReadOnlyList<UsageSummary>> GetRecentSummariesAsync(int days, CancellationToken cancellationToken = default)
    {
        var end = DateOnly.FromDateTime(_clock.Now.LocalDateTime);
        return _repository.GetRangeAsync(end.AddDays(-Math.Max(1, days) + 1), end, cancellationToken);
    }

    public async Task UpdateSettingsAsync(BreakSettings settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        await _settingsStore.SaveAsync(settings, cancellationToken);
    }

    public void ApplyTransientSettings(BreakSettings settings)
    {
        _settings = settings;
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        _timer.Dispose();

        if (_runtimeTask is not null)
        {
            await _runtimeTask;
        }

        await FlushUsageAsync(CancellationToken.None);

        _cancellation.Dispose();
    }
}
