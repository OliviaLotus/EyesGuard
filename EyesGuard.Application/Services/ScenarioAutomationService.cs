using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;
using EyesGuard.Core.Services;

namespace EyesGuard.Application.Services;

/// <summary>
/// 场景自动化服务 - 处理自动场景切换逻辑
/// </summary>
public class ScenarioAutomationService
{
    private readonly IProcessDetector _processDetector;
    private string? _currentAutomaticScenario;

    public ScenarioAutomationService(IProcessDetector processDetector)
    {
        _processDetector = processDetector;
    }

    /// <summary>
    /// 当前自动场景ID
    /// </summary>
    public string? CurrentScenario => _currentAutomaticScenario;

    /// <summary>
    /// 检测当前应该激活的场景
    /// </summary>
    public EyeGuardScene? DetectActiveScene(IReadOnlyList<EyeGuardScene> scenes)
    {
        return ScenarioModeSelector.SelectScene(scenes, _processDetector.IsAnyRunning);
    }

    /// <summary>
    /// 更新当前场景
    /// </summary>
    public bool UpdateCurrentScenario(string? scenarioId)
    {
        if (_currentAutomaticScenario == scenarioId)
        {
            return false; // 未变化
        }

        _currentAutomaticScenario = scenarioId;
        return true; // 已变化
    }

    /// <summary>
    /// 清除当前场景
    /// </summary>
    public void ClearCurrentScenario()
    {
        _currentAutomaticScenario = null;
    }

    /// <summary>
    /// 从场景创建设置
    /// </summary>
    public static BreakSettings CreateSettingsFromScene(EyeGuardScene scene, BreakSettings baseSettings)
    {
        return baseSettings with
        {
            IsEnabled = !scene.PauseReminders,
            WorkMinutes = scene.WorkMinutes,
            BreakSeconds = scene.BreakSeconds,
            BlueLightEnabled = scene.BlueLightEnabled,
            BlueLightIntensity = scene.BlueLightIntensity,
            StrictModeEnabled = scene.StrictModeEnabled,
            MaxContinuousWorkMinutes = scene.MaxContinuousWorkMinutes,
            NightRestrictionEnabled = scene.NightRestrictionEnabled,
            NightRestrictionStart = scene.NightRestrictionStart,
            NightRestrictionEnd = scene.NightRestrictionEnd
        };
    }
}
