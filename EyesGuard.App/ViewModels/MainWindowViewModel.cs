using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EyesGuard.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    // 子 ViewModels - 委托模式
    public BreakControlViewModel BreakControl { get; }
    public DashboardViewModel Dashboard { get; }
    public BlueLightViewModel BlueLight { get; }
    public SettingsViewModel Settings { get; }
    public SceneManagementViewModel SceneManagement { get; }
    public BrightnessViewModel Brightness { get; }
    public UpdateViewModel Update { get; }
    public StartupViewModel Startup { get; }
    public SyncViewModel Sync { get; }
    public WidgetViewModel Widget { get; }

    public MainWindowViewModel(
        BreakControlViewModel breakControl,
        DashboardViewModel dashboard,
        BlueLightViewModel blueLight,
        SettingsViewModel settings,
        SceneManagementViewModel sceneManagement,
        BrightnessViewModel brightness,
        UpdateViewModel update,
        StartupViewModel startup,
        SyncViewModel sync,
        WidgetViewModel widget)
    {
        BreakControl = breakControl;
        Dashboard = dashboard;
        BlueLight = blueLight;
        Settings = settings;
        SceneManagement = sceneManagement;
        Brightness = brightness;
        Update = update;
        Startup = startup;
        Sync = sync;
        Widget = widget;

        // 订阅子 ViewModel 事件
        BreakControl.BreakAttentionRequested += (_, _) => BreakAttentionRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? BreakAttentionRequested;

    // 保留的主窗口属性
    [ObservableProperty] private int _selectedPageIndex = 0;
    [ObservableProperty] private string _statusMessage = "护眼服务运行正常";

    public async Task InitializeAsync()
    {
        // 初始化子 ViewModels
        await Dashboard.InitializeAsync();

        // 启动自动调度
        BlueLight.StartScheduling();
        SceneManagement.StartScenarioDetection();
    }

    [RelayCommand]
    private void ToggleDesktopWidget() => Settings.DesktopWidgetEnabled = !Settings.DesktopWidgetEnabled;

    public async ValueTask DisposeAsync()
    {
        // 释放子 ViewModels
        BreakControl.Dispose();
        Dashboard.Dispose();
        Settings.Dispose();
        Widget.Dispose();
        BlueLight.Dispose();
        SceneManagement.Dispose();

        await Task.CompletedTask;
    }
}
