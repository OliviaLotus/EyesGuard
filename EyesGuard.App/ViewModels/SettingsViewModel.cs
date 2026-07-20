using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyesGuard.Core.Models;
using EyesGuard.Core.Services;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 设置视图模型 - 负责应用设置的管理和保存
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly BreakReminderService _breakService;
    private readonly SyncService _syncService;
    private bool _isApplyingSettings;
    private CancellationTokenSource? _settingsSaveCancellation;

    public SettingsViewModel(BreakReminderService breakService, SyncService syncService)
    {
        _breakService = breakService;
        _syncService = syncService;
        LoadFromSettings(breakService.Settings);
    }

    [ObservableProperty] private bool _isProtectionEnabled = true;
    [ObservableProperty] private int _workMinutes = 20;
    [ObservableProperty] private int _breakSeconds = 20;
    [ObservableProperty] private int _snoozeMinutes = 5;
    [ObservableProperty] private int _idleResetMinutes = 2;
    [ObservableProperty] private bool _strictModeEnabled;
    [ObservableProperty] private int _maxContinuousWorkMinutes = 40;
    [ObservableProperty] private bool _nightRestrictionEnabled;
    [ObservableProperty] private TimeSpan? _nightRestrictionStartTime = TimeSpan.FromHours(22);
    [ObservableProperty] private TimeSpan? _nightRestrictionEndTime = TimeSpan.FromHours(7);
    [ObservableProperty] private bool _doNotDisturbEnabled;
    [ObservableProperty] private TimeSpan? _doNotDisturbStartTime = TimeSpan.FromHours(22);
    [ObservableProperty] private TimeSpan? _doNotDisturbEndTime = TimeSpan.FromHours(7);
    [ObservableProperty] private bool _followSystemTheme = true;
    [ObservableProperty] private bool _desktopWidgetEnabled;
    [ObservableProperty] private string _updateManifestUrl = string.Empty;
    [ObservableProperty] private string _statusMessage = "护眼服务运行正常";

    public bool CanDeferBreak => !StrictModeEnabled;

    partial void OnIsProtectionEnabledChanged(bool value) => QueueSettingsSave();
    partial void OnWorkMinutesChanged(int value) => QueueSettingsSave();
    partial void OnBreakSecondsChanged(int value) => QueueSettingsSave();
    partial void OnSnoozeMinutesChanged(int value) => QueueSettingsSave();
    partial void OnIdleResetMinutesChanged(int value) => QueueSettingsSave();
    partial void OnStrictModeEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDeferBreak));
        QueueSettingsSave();
    }
    partial void OnMaxContinuousWorkMinutesChanged(int value) => QueueSettingsSave();
    partial void OnNightRestrictionEnabledChanged(bool value) => QueueSettingsSave();
    partial void OnNightRestrictionStartTimeChanged(TimeSpan? value) => QueueSettingsSave();
    partial void OnNightRestrictionEndTimeChanged(TimeSpan? value) => QueueSettingsSave();
    partial void OnDoNotDisturbEnabledChanged(bool value) => QueueSettingsSave();
    partial void OnDoNotDisturbStartTimeChanged(TimeSpan? value) => QueueSettingsSave();
    partial void OnDoNotDisturbEndTimeChanged(TimeSpan? value) => QueueSettingsSave();
    partial void OnFollowSystemThemeChanged(bool value) => QueueSettingsSave();
    partial void OnDesktopWidgetEnabledChanged(bool value) => QueueSettingsSave();
    partial void OnUpdateManifestUrlChanged(string value) => QueueSettingsSave();

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (_isApplyingSettings)
        {
            return;
        }

        _settingsSaveCancellation?.Cancel();
        await SaveSettingsCoreAsync(CancellationToken.None);
        StatusMessage = "设置已保存";
    }

    private void QueueSettingsSave()
    {
        if (_isApplyingSettings)
        {
            return;
        }

        _settingsSaveCancellation?.Cancel();
        _settingsSaveCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _settingsSaveCancellation = cancellation;
        _ = DebouncedSaveAsync(cancellation.Token);
    }

    private async Task DebouncedSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken);
            await SaveSettingsCoreAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task SaveSettingsCoreAsync(CancellationToken cancellationToken)
    {
        var settings = CreateSettings();
        await _breakService.UpdateSettingsAsync(settings, cancellationToken);
        var usage = await _breakService.GetRecentSummariesAsync(30, cancellationToken);
        await _syncService.QueueSnapshotAsync(settings, usage, cancellationToken);
    }

    public BreakSettings CreateSettings() => new()
    {
        IsEnabled = IsProtectionEnabled,
        WorkMinutes = WorkMinutes,
        BreakSeconds = BreakSeconds,
        SnoozeMinutes = SnoozeMinutes,
        IdleResetMinutes = IdleResetMinutes,
        StrictModeEnabled = StrictModeEnabled,
        MaxContinuousWorkMinutes = MaxContinuousWorkMinutes,
        NightRestrictionEnabled = NightRestrictionEnabled,
        NightRestrictionStart = ToTimeOnly(NightRestrictionStartTime, TimeOnly.FromTimeSpan(TimeSpan.FromHours(22))),
        NightRestrictionEnd = ToTimeOnly(NightRestrictionEndTime, TimeOnly.FromTimeSpan(TimeSpan.FromHours(7))),
        DoNotDisturbEnabled = DoNotDisturbEnabled,
        DoNotDisturbStart = ToTimeOnly(DoNotDisturbStartTime, TimeOnly.FromTimeSpan(TimeSpan.FromHours(22))),
        DoNotDisturbEnd = ToTimeOnly(DoNotDisturbEndTime, TimeOnly.FromTimeSpan(TimeSpan.FromHours(7))),
        FollowSystemTheme = FollowSystemTheme,
        DesktopWidgetEnabled = DesktopWidgetEnabled,
        UpdateManifestUrl = UpdateManifestUrl,
        BlueLightEnabled = false,
        BlueLightIntensity = 40,
        BlueLightScheduleEnabled = false,
        BlueLightScheduleStart = TimeOnly.FromTimeSpan(TimeSpan.FromHours(20)),
        BlueLightScheduleEnd = TimeOnly.FromTimeSpan(TimeSpan.FromHours(7)),
        ColorVisionMode = ColorVisionMode.None,
        StartWithWindows = false,
        ScenarioAutomationEnabled = false,
        OfficeProcessNames = string.Empty,
        ReadingProcessNames = string.Empty,
        MovieProcessNames = string.Empty,
        ChildProcessNames = string.Empty,
        Scenes = []
    };

    public void LoadFromSettings(BreakSettings settings)
    {
        _isApplyingSettings = true;
        IsProtectionEnabled = settings.IsEnabled;
        WorkMinutes = settings.WorkMinutes;
        BreakSeconds = settings.BreakSeconds;
        SnoozeMinutes = settings.SnoozeMinutes;
        IdleResetMinutes = settings.IdleResetMinutes;
        StrictModeEnabled = settings.StrictModeEnabled;
        MaxContinuousWorkMinutes = settings.MaxContinuousWorkMinutes;
        NightRestrictionEnabled = settings.NightRestrictionEnabled;
        NightRestrictionStartTime = settings.NightRestrictionStart.ToTimeSpan();
        NightRestrictionEndTime = settings.NightRestrictionEnd.ToTimeSpan();
        DoNotDisturbEnabled = settings.DoNotDisturbEnabled;
        DoNotDisturbStartTime = settings.DoNotDisturbStart.ToTimeSpan();
        DoNotDisturbEndTime = settings.DoNotDisturbEnd.ToTimeSpan();
        FollowSystemTheme = settings.FollowSystemTheme;
        DesktopWidgetEnabled = settings.DesktopWidgetEnabled;
        UpdateManifestUrl = settings.UpdateManifestUrl;
        _isApplyingSettings = false;
    }

    private static TimeOnly ToTimeOnly(TimeSpan? value, TimeOnly fallback)
    {
        if (value is null)
        {
            return fallback;
        }

        var normalized = value.Value.TotalMinutes % (24 * 60);
        if (normalized < 0)
        {
            normalized += 24 * 60;
        }

        return new TimeOnly((int)(normalized / 60), (int)(normalized % 60));
    }

    public void Dispose()
    {
        _settingsSaveCancellation?.Cancel();
        _settingsSaveCancellation?.Dispose();
    }
}
