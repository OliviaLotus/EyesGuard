using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;
using EyesGuard.Core.Services;
using EyesGuard.Application.Services;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 场景管理视图模型 - 负责场景编辑和自动化
/// </summary>
public partial class SceneManagementViewModel : ViewModelBase, IDisposable
{
    private readonly BreakReminderService _breakService;
    private readonly ScenarioAutomationService _scenarioService;
    private readonly IProcessDetector _processDetector;
    private readonly DispatcherTimer _scenarioTimer;
    private BreakSettings? _settingsBeforeAutomaticScenario;
    private string? _automaticScenario;
    private bool _isApplyingAutomaticScenario;

    public SceneManagementViewModel(
        BreakReminderService breakService,
        ScenarioAutomationService scenarioService,
        IProcessDetector processDetector)
    {
        _breakService = breakService;
        _scenarioService = scenarioService;
        _processDetector = processDetector;

        var settings = breakService.Settings;
        ScenarioAutomationEnabled = settings.ScenarioAutomationEnabled;
        OfficeProcessNames = settings.OfficeProcessNames;
        ReadingProcessNames = settings.ReadingProcessNames;
        MovieProcessNames = settings.MovieProcessNames;
        ChildProcessNames = settings.ChildProcessNames;

        foreach (var scene in settings.Scenes)
        {
            Scenes.Add(new SceneViewModel(scene));
        }
        SelectedScene = Scenes.FirstOrDefault();

        // 定时器用于自动场景检测
        _scenarioTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _scenarioTimer.Tick += (_, _) => ApplyAutomaticScenario();
    }

    [ObservableProperty] private bool _scenarioAutomationEnabled;
    [ObservableProperty] private string _officeProcessNames = "devenv,code,excel,winword";
    [ObservableProperty] private string _readingProcessNames = "kindle,book,edge,chrome";
    [ObservableProperty] private string _movieProcessNames = "vlc,potplayer,mpc-hc,netflix";
    [ObservableProperty] private string _childProcessNames = "minecraft,roblox";
    [ObservableProperty] private string _scenarioStatusText = "自动场景未启用";
    [ObservableProperty] private bool _isAutomaticScenarioActive;
    [ObservableProperty] private SceneViewModel? _selectedScene;
    [ObservableProperty] private string _sceneEditorStatus = "选择一个场景进行编辑";
    [ObservableProperty] private string _activeModeName = "办公模式";

    public ObservableCollection<SceneViewModel> Scenes { get; } = [];
    public bool IsSceneEditingEnabled => !IsAutomaticScenarioActive;

    partial void OnScenarioAutomationEnabledChanged(bool value)
    {
        if (_isApplyingAutomaticScenario)
        {
            return;
        }

        if (!value)
        {
            RestoreManualScenarioSettings();
        }
        else
        {
            ApplyAutomaticScenario();
        }
    }

    partial void OnOfficeProcessNamesChanged(string value)
    {
        UpdateBuiltInSceneProcessNames("office", value);
    }

    partial void OnReadingProcessNamesChanged(string value)
    {
        UpdateBuiltInSceneProcessNames("reading", value);
    }

    partial void OnMovieProcessNamesChanged(string value)
    {
        UpdateBuiltInSceneProcessNames("movie", value);
    }

    partial void OnChildProcessNamesChanged(string value)
    {
        UpdateBuiltInSceneProcessNames("child", value);
    }

    partial void OnSelectedSceneChanged(SceneViewModel? value)
    {
        if (value is null)
        {
            SceneEditorStatus = "选择一个场景进行编辑";
        }
        else
        {
            SceneEditorStatus = $"正在编辑\"{value.Name}\"";
        }
    }

    partial void OnIsAutomaticScenarioActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSceneEditingEnabled));
    }

    [RelayCommand]
    private void AddScene()
    {
        if (!IsSceneEditingEnabled)
        {
            SceneEditorStatus = "自动场景运行期间无法编辑场景";
            return;
        }

        var scene = new SceneViewModel(new EyeGuardScene { Id = Guid.NewGuid().ToString("N"), Name = "新场景" });
        Scenes.Add(scene);
        SelectedScene = scene;
        SceneEditorStatus = "已添加场景，请填写参数后保存";
    }

    [RelayCommand]
    private void SaveScene()
    {
        if (!IsSceneEditingEnabled)
        {
            SceneEditorStatus = "自动场景运行期间无法编辑场景";
            return;
        }

        if (SelectedScene is null)
        {
            SceneEditorStatus = "请先选择场景";
            return;
        }

        var replacement = new SceneViewModel(SelectedScene.ToModel());
        var index = Scenes.IndexOf(SelectedScene);
        if (index < 0)
        {
            SceneEditorStatus = "场景已不存在，请重新选择";
            SelectedScene = Scenes.FirstOrDefault();
            return;
        }

        Scenes[index] = replacement;
        SelectedScene = replacement;
        SceneEditorStatus = $"已保存\"{replacement.Name}\"";
    }

    [RelayCommand]
    private void DeleteScene()
    {
        if (!IsSceneEditingEnabled)
        {
            SceneEditorStatus = "自动场景运行期间无法编辑场景";
            return;
        }

        if (SelectedScene is null) return;
        if (Scenes.Count <= 1)
        {
            SceneEditorStatus = "至少保留一个场景，无法删除";
            return;
        }

        var deleted = SelectedScene;
        Scenes.Remove(deleted);
        SelectedScene = Scenes.FirstOrDefault();
        SceneEditorStatus = $"已删除\"{deleted.Name}\"";
    }

    public void ApplyAutomaticScenario()
    {
        if (!ScenarioAutomationEnabled)
        {
            RestoreManualScenarioSettings();
            ScenarioStatusText = "自动场景未启用";
            return;
        }

        var detected = DetectScenario();
        if (detected == _automaticScenario)
        {
            return;
        }

        if (detected is null)
        {
            RestoreManualScenarioSettings();
            ScenarioStatusText = "未检测到匹配程序，使用手动设置";
            return;
        }

        if (_settingsBeforeAutomaticScenario is null)
        {
            _settingsBeforeAutomaticScenario = _breakService.Settings;
            var sceneName = _settingsBeforeAutomaticScenario.Scenes
                .FirstOrDefault(s => s.Id == detected)?.Name ?? "自定义方案";
            ActiveModeName = sceneName;
        }

        _automaticScenario = detected;
        var scene = _settingsBeforeAutomaticScenario.Scenes.FirstOrDefault(item => item.Id == detected);
        if (scene is null)
        {
            RestoreManualScenarioSettings();
            ScenarioStatusText = "场景配置不存在，已恢复手动设置";
            return;
        }

        var scenarioSettings = CreateSettingsFromScene(scene, _settingsBeforeAutomaticScenario);
        _isApplyingAutomaticScenario = true;
        try
        {
            IsAutomaticScenarioActive = true;
            ActiveModeName = scene.Name;
            _breakService.ApplyTransientSettings(scenarioSettings);
            ScenarioStatusText = $"已自动切换：{ActiveModeName}";
        }
        finally
        {
            _isApplyingAutomaticScenario = false;
        }
    }

    private string? DetectScenario()
    {
        var allScenes = Scenes.Select(s => s.ToModel()).ToArray();
        var scene = ScenarioModeSelector.SelectScene(allScenes, _processDetector.IsAnyRunning);
        return scene?.Id;
    }

    private void RestoreManualScenarioSettings()
    {
        if (_settingsBeforeAutomaticScenario is null)
        {
            _automaticScenario = null;
            IsAutomaticScenarioActive = false;
            return;
        }

        var settings = _settingsBeforeAutomaticScenario with
        {
            ScenarioAutomationEnabled = ScenarioAutomationEnabled,
            OfficeProcessNames = OfficeProcessNames,
            ReadingProcessNames = ReadingProcessNames,
            MovieProcessNames = MovieProcessNames,
            ChildProcessNames = ChildProcessNames
        };

        _settingsBeforeAutomaticScenario = null;
        _automaticScenario = null;

        _isApplyingAutomaticScenario = true;
        try
        {
            _breakService.ApplyTransientSettings(settings);
            ActiveModeName = "自定义方案";
            IsAutomaticScenarioActive = false;
        }
        finally
        {
            _isApplyingAutomaticScenario = false;
        }
    }

    private static BreakSettings CreateSettingsFromScene(EyeGuardScene scene, BreakSettings source) => source with
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

    private void UpdateBuiltInSceneProcessNames(string sceneId, string processNames)
    {
        if (_isApplyingAutomaticScenario)
        {
            return;
        }

        var scene = Scenes.FirstOrDefault(item =>
            string.Equals(item.Id, sceneId, StringComparison.OrdinalIgnoreCase));
        if (scene is not null && !string.Equals(scene.ProcessNames, processNames, StringComparison.Ordinal))
        {
            scene.ProcessNames = processNames;
        }
    }

    [RelayCommand]
    private void ApplySelectedScene()
    {
        if (!IsSceneEditingEnabled)
        {
            SceneEditorStatus = "自动场景运行期间无法编辑场景";
            return;
        }

        if (SelectedScene is null) return;
        ApplyScene(SelectedScene.ToModel());
    }

    [RelayCommand]
    private void ApplyMode(string? mode)
    {
        if (!IsSceneEditingEnabled)
        {
            SceneEditorStatus = "自动场景运行期间无法切换手动场景";
            return;
        }

        var sceneId = mode switch
        {
            "Reading" => "reading",
            "Movie" => "movie",
            "Child" => "child",
            _ => "office"
        };
        var scene = Scenes.FirstOrDefault(item =>
            string.Equals(item.Id, sceneId, StringComparison.OrdinalIgnoreCase));
        if (scene is null)
        {
            SceneEditorStatus = "预设场景不存在，请重新加载设置";
            return;
        }

        SelectedScene = scene;
        ApplyScene(scene.ToModel());
    }

    private void ApplyScene(EyeGuardScene scene)
    {
        var scenarioSettings = CreateSettingsFromScene(scene, _breakService.Settings);
        var appliedScene = Scenes.FirstOrDefault(item =>
            string.Equals(item.Id, scene.Id, StringComparison.OrdinalIgnoreCase));
        if (appliedScene is not null)
        {
            SelectedScene = appliedScene;
        }
        ActiveModeName = scene.Name;
        _breakService.ApplyTransientSettings(scenarioSettings);
        SceneEditorStatus = $"已应用\"{scene.Name}\"";
    }

    /// <summary>
    /// 启动自动场景检测
    /// </summary>
    public void StartScenarioDetection()
    {
        _scenarioTimer.Start();
        ApplyAutomaticScenario();
    }

    public void Dispose()
    {
        _scenarioTimer.Stop();
    }
}
