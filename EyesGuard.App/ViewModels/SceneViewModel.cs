using CommunityToolkit.Mvvm.ComponentModel;
using EyesGuard.Core.Models;

namespace EyesGuard.App.ViewModels;

public partial class SceneViewModel : ObservableObject
{
    public SceneViewModel(EyeGuardScene scene)
    {
        Id = scene.Id;
        Name = scene.Name;
        ProcessNames = scene.ProcessNames;
        WorkMinutes = scene.WorkMinutes;
        BreakSeconds = scene.BreakSeconds;
        PauseReminders = scene.PauseReminders;
        BlueLightEnabled = scene.BlueLightEnabled;
        BlueLightIntensity = scene.BlueLightIntensity;
        BrightnessPercent = scene.BrightnessPercent;
        StrictModeEnabled = scene.StrictModeEnabled;
        MaxContinuousWorkMinutes = scene.MaxContinuousWorkMinutes;
        NightRestrictionEnabled = scene.NightRestrictionEnabled;
        NightRestrictionStart = scene.NightRestrictionStart.ToTimeSpan();
        NightRestrictionEnd = scene.NightRestrictionEnd.ToTimeSpan();
    }

    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _name = "自定义场景";
    [ObservableProperty] private string _processNames = string.Empty;
    [ObservableProperty] private int _workMinutes = 25;
    [ObservableProperty] private int _breakSeconds = 20;
    [ObservableProperty] private bool _pauseReminders;
    [ObservableProperty] private bool _blueLightEnabled = true;
    [ObservableProperty] private int _blueLightIntensity = 35;
    [ObservableProperty] private int _brightnessPercent = 70;
    [ObservableProperty] private bool _strictModeEnabled;
    [ObservableProperty] private int _maxContinuousWorkMinutes = 40;
    [ObservableProperty] private bool _nightRestrictionEnabled;
    [ObservableProperty] private TimeSpan? _nightRestrictionStart = TimeSpan.FromHours(22);
    [ObservableProperty] private TimeSpan? _nightRestrictionEnd = TimeSpan.FromHours(7);

    public EyeGuardScene ToModel() => new EyeGuardScene
    {
        Id = Id,
        Name = Name,
        ProcessNames = ProcessNames,
        WorkMinutes = WorkMinutes,
        BreakSeconds = BreakSeconds,
        PauseReminders = PauseReminders,
        BlueLightEnabled = BlueLightEnabled,
        BlueLightIntensity = BlueLightIntensity,
        BrightnessPercent = BrightnessPercent,
        StrictModeEnabled = StrictModeEnabled,
        MaxContinuousWorkMinutes = MaxContinuousWorkMinutes,
        NightRestrictionEnabled = NightRestrictionEnabled,
        NightRestrictionStart = ToTimeOnly(NightRestrictionStart, new TimeOnly(22, 0)),
        NightRestrictionEnd = ToTimeOnly(NightRestrictionEnd, new TimeOnly(7, 0))
    }.Normalize();

    private static TimeOnly ToTimeOnly(TimeSpan? value, TimeOnly fallback)
    {
        if (value is null) return fallback;
        var minutes = value.Value.TotalMinutes % (24 * 60);
        if (minutes < 0) minutes += 24 * 60;
        return new TimeOnly((int)(minutes / 60), (int)(minutes % 60));
    }
}
