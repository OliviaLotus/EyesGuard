using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyesGuard.Core.Services;
using EyesGuard.Core.Models;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 休息控制视图模型 - 负责休息相关的 UI 状态和命令
/// </summary>
public partial class BreakControlViewModel : ViewModelBase
{
    private readonly BreakReminderService _breakService;

    public BreakControlViewModel(BreakReminderService breakService)
    {
        _breakService = breakService;
        _breakService.SnapshotChanged += OnSnapshotChanged;
        _breakService.BreakDue += OnBreakDue;
    }

    public event EventHandler? BreakAttentionRequested;

    [ObservableProperty] private string _phaseText = "准备中";
    [ObservableProperty] private string _sessionText = "0分钟";
    [ObservableProperty] private string _nextBreakText = "20:00";
    [ObservableProperty] private string _restRemainingText = "20";
    [ObservableProperty] private double _workProgress;
    [ObservableProperty] private bool _isBreakPrompt;
    [ObservableProperty] private bool _isResting;
    [ObservableProperty] private bool _strictModeEnabled;

    public bool CanDeferBreak => !StrictModeEnabled;

    [RelayCommand]
    private void StartBreak()
    {
        var snapshot = _breakService.StartBreak();
        ApplySnapshot(snapshot);
    }

    [RelayCommand]
    private void Snooze()
    {
        var snapshot = _breakService.Snooze();
        ApplySnapshot(snapshot);
    }

    [RelayCommand]
    private void Skip()
    {
        var snapshot = _breakService.Skip();
        ApplySnapshot(snapshot);
    }

    partial void OnStrictModeEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDeferBreak));
    }

    private void OnSnapshotChanged(object? sender, EyeGuardSnapshot snapshot)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplySnapshot(snapshot));
    }

    private void OnBreakDue(object? sender, EventArgs args)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => BreakAttentionRequested?.Invoke(this, EventArgs.Empty));
    }

    private void ApplySnapshot(EyeGuardSnapshot snapshot)
    {
        PhaseText = snapshot.Phase switch
        {
            BreakPhase.Working => "状态良好",
            BreakPhase.Idle => "已暂停计时",
            BreakPhase.Prompt => "该休息了",
            BreakPhase.Resting => "正在休息",
            BreakPhase.NightRestricted => "夜间已禁用",
            _ => "护眼已暂停"
        };
        SessionText = FormatDuration(snapshot.SessionElapsed);
        NextBreakText = snapshot.NextBreakIn.ToString(@"mm\:ss");
        RestRemainingText = Math.Max(0, (int)Math.Ceiling(snapshot.RestRemaining.TotalSeconds)).ToString();
        WorkProgress = snapshot.WorkProgress * 100;
        IsBreakPrompt = snapshot.Phase == BreakPhase.Prompt;
        IsResting = snapshot.Phase == BreakPhase.Resting;
    }

    private static string FormatDuration(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}小时{value.Minutes}分钟"
        : $"{Math.Max(0, (int)value.TotalMinutes)}分钟";

    public void Dispose()
    {
        _breakService.SnapshotChanged -= OnSnapshotChanged;
        _breakService.BreakDue -= OnBreakDue;
    }
}
