namespace EyesGuard.Core.Models;

public sealed record EyeGuardScene
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "自定义场景";
    public string ProcessNames { get; init; } = string.Empty;
    public int WorkMinutes { get; init; } = 25;
    public int BreakSeconds { get; init; } = 20;
    public bool PauseReminders { get; init; }
    public bool BlueLightEnabled { get; init; } = true;
    public int BlueLightIntensity { get; init; } = 35;
    public int BrightnessPercent { get; init; } = 70;
    public bool StrictModeEnabled { get; init; }
    public int MaxContinuousWorkMinutes { get; init; } = 40;
    public bool NightRestrictionEnabled { get; init; }
    public TimeOnly NightRestrictionStart { get; init; } = new(22, 0);
    public TimeOnly NightRestrictionEnd { get; init; } = new(7, 0);

    public EyeGuardScene Normalize() => this with
    {
        Id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("N") : Id,
        Name = string.IsNullOrWhiteSpace(Name) ? "未命名场景" : Name.Trim(),
        ProcessNames = ProcessNames?.Trim() ?? string.Empty,
        WorkMinutes = Math.Clamp(WorkMinutes, 1, 180),
        BreakSeconds = Math.Clamp(BreakSeconds, 10, 600),
        BlueLightIntensity = Math.Clamp(BlueLightIntensity, 0, 100),
        BrightnessPercent = Math.Clamp(BrightnessPercent, 0, 100),
        MaxContinuousWorkMinutes = Math.Clamp(MaxContinuousWorkMinutes, 1, 480)
    };

    public static IReadOnlyList<EyeGuardScene> DefaultScenes { get; } =
    [
        new EyeGuardScene
        {
            Id = "child", Name = "儿童模式", ProcessNames = "minecraft,roblox", WorkMinutes = 30,
            BreakSeconds = 60, BlueLightIntensity = 45, BrightnessPercent = 55, StrictModeEnabled = true,
            MaxContinuousWorkMinutes = 30, NightRestrictionEnabled = true
        },
        new EyeGuardScene
        {
            Id = "movie", Name = "观影模式", ProcessNames = "vlc,potplayer,mpc-hc,netflix", WorkMinutes = 60,
            PauseReminders = true, BlueLightIntensity = 30, BrightnessPercent = 50
        },
        new EyeGuardScene
        {
            Id = "reading", Name = "阅读模式", ProcessNames = "kindle,book,edge,chrome", WorkMinutes = 40,
            BreakSeconds = 30, BlueLightIntensity = 55, BrightnessPercent = 60
        },
        new EyeGuardScene
        {
            Id = "office", Name = "办公模式", ProcessNames = "devenv,code,excel,winword", WorkMinutes = 25,
            BlueLightIntensity = 35, BrightnessPercent = 70
        }
    ];
}
