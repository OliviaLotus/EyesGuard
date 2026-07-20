using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EyesGuard.Application.Services;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 蓝光管理视图模型 - 负责蓝光过滤和色觉辅助的设置
/// </summary>
public partial class BlueLightViewModel : ViewModelBase, IDisposable
{
    private readonly BlueLightSchedulingService _schedulingService;
    private readonly DispatcherTimer _scheduleTimer;
    private bool _isApplyingSchedule;

    public BlueLightViewModel(BlueLightSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;

        // 定时器用于自动调度
        _scheduleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _scheduleTimer.Tick += (_, _) => ApplyAutomaticSchedule();
    }

    [ObservableProperty] private bool _blueLightEnabled;
    [ObservableProperty] private int _blueLightIntensity = 40;
    [ObservableProperty] private bool _blueLightScheduleEnabled;
    [ObservableProperty] private TimeSpan? _blueLightScheduleStartTime = TimeSpan.FromHours(20);
    [ObservableProperty] private TimeSpan? _blueLightScheduleEndTime = TimeSpan.FromHours(7);
    [ObservableProperty] private Core.Models.ColorVisionMode _colorVisionMode;

    public IReadOnlyList<ColorVisionModeOption> ColorVisionModeOptions { get; } =
    [
        new(Core.Models.ColorVisionMode.None, "关闭", "不应用色觉辅助覆盖层"),
        new(Core.Models.ColorVisionMode.Protanopia, "红色盲辅助", "增强红绿色差异，适合红色感知较弱"),
        new(Core.Models.ColorVisionMode.Deuteranopia, "绿色盲辅助", "增强红绿色差异，适合绿色感知较弱"),
        new(Core.Models.ColorVisionMode.Tritanopia, "蓝黄色弱辅助", "增强蓝黄色差异，适合蓝黄色辨识困难")
    ];

    public ColorVisionModeOption SelectedColorVisionModeOption
    {
        get => ColorVisionModeOptions.First(option => option.Mode == ColorVisionMode);
        set
        {
            if (value.Mode != ColorVisionMode)
            {
                ColorVisionMode = value.Mode;
            }
        }
    }

    partial void OnBlueLightEnabledChanged(bool value)
    {
        if (!_isApplyingSchedule)
        {
            _schedulingService.ManualOverride = value;
        }
        OnSettingsChanged();
    }

    partial void OnBlueLightIntensityChanged(int value)
    {
        OnSettingsChanged();
    }

    partial void OnBlueLightScheduleEnabledChanged(bool value)
    {
        _schedulingService.ClearManualOverride();
        ApplyAutomaticSchedule();
        OnSettingsChanged();
    }

    partial void OnBlueLightScheduleStartTimeChanged(TimeSpan? value)
    {
        ApplyAutomaticSchedule();
        OnSettingsChanged();
    }

    partial void OnBlueLightScheduleEndTimeChanged(TimeSpan? value)
    {
        ApplyAutomaticSchedule();
        OnSettingsChanged();
    }

    partial void OnColorVisionModeChanged(Core.Models.ColorVisionMode value)
    {
        OnPropertyChanged(nameof(SelectedColorVisionModeOption));
        OnSettingsChanged();
    }

    public event EventHandler? SettingsChanged;

    private void OnSettingsChanged()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 启动自动调度
    /// </summary>
    public void StartScheduling()
    {
        _scheduleTimer.Start();
        ApplyAutomaticSchedule();
    }

    /// <summary>
    /// 应用自动调度逻辑
    /// </summary>
    public void ApplyAutomaticSchedule()
    {
        var now = DateTime.Now.TimeOfDay;
        var start = BlueLightScheduleStartTime ?? TimeSpan.FromHours(20);
        var end = BlueLightScheduleEndTime ?? TimeSpan.FromHours(7);

        var shouldEnable = _schedulingService.ShouldEnableBlueLightFilter(
            BlueLightScheduleEnabled, now, start, end);

        if (shouldEnable && BlueLightEnabled != shouldEnable)
        {
            _isApplyingSchedule = true;
            try
            {
                BlueLightEnabled = shouldEnable;
            }
            finally
            {
                _isApplyingSchedule = false;
            }
        }
    }

    /// <summary>
    /// 从设置加载
    /// </summary>
    public void LoadFromSettings(Core.Models.BreakSettings settings)
    {
        BlueLightEnabled = settings.BlueLightEnabled;
        BlueLightIntensity = settings.BlueLightIntensity;
        BlueLightScheduleEnabled = settings.BlueLightScheduleEnabled;
        BlueLightScheduleStartTime = settings.BlueLightScheduleStart.ToTimeSpan();
        BlueLightScheduleEndTime = settings.BlueLightScheduleEnd.ToTimeSpan();
        ColorVisionMode = settings.ColorVisionMode;
    }

    public void Dispose()
    {
        _scheduleTimer.Stop();
    }
}
