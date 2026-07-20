namespace EyesGuard.Core.Models;

public sealed record BreakSettings
{
    public bool IsEnabled { get; init; } = true;
    public int WorkMinutes { get; init; } = 20;
    public int BreakSeconds { get; init; } = 20;
    public int SnoozeMinutes { get; init; } = 5;
    public int IdleResetMinutes { get; init; } = 2;
    public bool StrictModeEnabled { get; init; }
    public int MaxContinuousWorkMinutes { get; init; } = 40;
    public bool NightRestrictionEnabled { get; init; }
    public TimeOnly NightRestrictionStart { get; init; } = new(22, 0);
    public TimeOnly NightRestrictionEnd { get; init; } = new(7, 0);
    public bool BlueLightEnabled { get; init; }
    public int BlueLightIntensity { get; init; } = 40;
    public bool BlueLightScheduleEnabled { get; init; }
    public TimeOnly BlueLightScheduleStart { get; init; } = new(20, 0);
    public TimeOnly BlueLightScheduleEnd { get; init; } = new(7, 0);
    public bool FollowSystemTheme { get; init; } = true;
    public ColorVisionMode ColorVisionMode { get; init; }
    public bool DesktopWidgetEnabled { get; init; }
    public bool StartWithWindows { get; init; }
    public string UpdateManifestUrl { get; init; } = string.Empty;
    public bool ScenarioAutomationEnabled { get; init; }
    public IReadOnlyList<EyeGuardScene> Scenes { get; init; } = [];
    public string OfficeProcessNames { get; init; } = "devenv,code,excel,winword";
    public string ReadingProcessNames { get; init; } = "kindle,book,edge,chrome";
    public string MovieProcessNames { get; init; } = "vlc,potplayer,mpc-hc,netflix";
    public string ChildProcessNames { get; init; } = "minecraft,roblox";
    public bool DoNotDisturbEnabled { get; init; }
    public TimeOnly DoNotDisturbStart { get; init; } = new(22, 0);
    public TimeOnly DoNotDisturbEnd { get; init; } = new(7, 0);

    public BreakSettings Normalize() => this with
    {
        WorkMinutes = Math.Clamp(WorkMinutes, 1, 180),
        BreakSeconds = Math.Clamp(BreakSeconds, 10, 600),
        SnoozeMinutes = Math.Clamp(SnoozeMinutes, 1, 60),
        IdleResetMinutes = Math.Clamp(IdleResetMinutes, 1, 30),
        MaxContinuousWorkMinutes = Math.Clamp(MaxContinuousWorkMinutes, 1, 480),
        BlueLightIntensity = Math.Clamp(BlueLightIntensity, 0, 100),
        ColorVisionMode = Enum.IsDefined(typeof(ColorVisionMode), ColorVisionMode)
            ? ColorVisionMode
            : EyesGuard.Core.Models.ColorVisionMode.None,
        Scenes = NormalizeScenes(Scenes, OfficeProcessNames, ReadingProcessNames, MovieProcessNames, ChildProcessNames)
    };

    private static IReadOnlyList<EyeGuardScene> NormalizeScenes(IReadOnlyList<EyeGuardScene>? scenes,
        string officeProcesses, string readingProcesses, string movieProcesses, string childProcesses)
    {
        if (scenes is { Count: > 0 })
        {
            return scenes.Select(scene => scene.Normalize()).ToArray();
        }

        return EyeGuardScene.DefaultScenes.Select(scene => scene with
        {
            ProcessNames = scene.Id switch
            {
                "office" => officeProcesses,
                "reading" => readingProcesses,
                "movie" => movieProcesses,
                "child" => childProcesses,
                _ => scene.ProcessNames
            }
        }).ToArray();
    }
}
