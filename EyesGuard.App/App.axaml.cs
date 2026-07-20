using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;
using EyesGuard.App.ViewModels;
using EyesGuard.App.Views;
using EyesGuard.App.Services;
using EyesGuard.App.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EyesGuard.App;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _services = new ServiceCollection()
                .AddCore()
                .AddInfrastructure()
                .AddPlatform()
                .AddApplication()
                .AddPresentation()
                .BuildServiceProvider();

            var viewModel = _services.GetRequiredService<MainWindowViewModel>();

            var mainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            var desktopWidget = new DesktopWidgetManager(mainWindow, viewModel);
            var blueLightOverlay = new BlueLightOverlayManager(mainWindow);
            blueLightOverlay.Update(viewModel.BlueLight.BlueLightEnabled, viewModel.BlueLight.BlueLightIntensity, viewModel.BlueLight.ColorVisionMode);
            viewModel.BlueLight.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(BlueLightViewModel.BlueLightEnabled)
                    or nameof(BlueLightViewModel.BlueLightIntensity)
                    or nameof(BlueLightViewModel.ColorVisionMode))
                {
                    blueLightOverlay.Update(viewModel.BlueLight.BlueLightEnabled, viewModel.BlueLight.BlueLightIntensity, viewModel.BlueLight.ColorVisionMode);
                }
            };
            desktop.MainWindow = mainWindow;
            Program.InstanceGuard!.SecondInstanceRequested += (_, _) =>
                Dispatcher.UIThread.Post(mainWindow.ActivateFromExternalRequest);
            ConfigureTray(desktop, mainWindow, viewModel);
            desktop.Exit += (_, _) =>
            {
                desktopWidget.Dispose();
                blueLightOverlay.Dispose();
                viewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _services.Dispose();
            };
            viewModel.InitializeAsync().GetAwaiter().GetResult();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureTray(IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow,
        MainWindowViewModel viewModel)
    {
        var tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://EyesGuard.App/Assets/avalonia-logo.ico"))),
            ToolTipText = "护眼助手"
        };
        var menu = new NativeMenu();
        var open = new NativeMenuItem("打开护眼助手");
        open.Click += (_, _) => mainWindow.ActivateFromExternalRequest();
        var breakNow = new NativeMenuItem("立即休息");
        breakNow.Click += (_, _) => viewModel.BreakControl.StartBreakCommand.Execute(null);
        var toggle = new NativeMenuItem("暂停护眼");
        toggle.ToggleType = MenuItemToggleType.CheckBox;
        toggle.IsChecked = !viewModel.Settings.IsProtectionEnabled;
        toggle.Click += (_, _) => viewModel.Settings.IsProtectionEnabled = !viewModel.Settings.IsProtectionEnabled;
        var widget = new NativeMenuItem("显示桌面小部件");
        widget.ToggleType = MenuItemToggleType.CheckBox;
        widget.IsChecked = viewModel.Settings.DesktopWidgetEnabled;
        widget.Click += (_, _) => viewModel.Settings.DesktopWidgetEnabled = !viewModel.Settings.DesktopWidgetEnabled;
        var exit = new NativeMenuItem("退出");
        exit.Click += (_, _) => mainWindow.CloseFromTray();
        menu.Items.Add(open);
        menu.Items.Add(breakNow);
        menu.Items.Add(toggle);
        menu.Items.Add(widget);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exit);
        tray.Menu = menu;
        TrayIcon.SetIcons(App.Current!, new TrayIcons { tray });
        viewModel.Settings.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.DesktopWidgetEnabled))
            {
                widget.IsChecked = viewModel.Settings.DesktopWidgetEnabled;
            }
        };
        viewModel.BreakControl.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(BreakControlViewModel.PhaseText)
                or nameof(BreakControlViewModel.NextBreakText))
            {
                tray.ToolTipText = $"护眼助手 · {viewModel.BreakControl.PhaseText} · 下次休息 {viewModel.BreakControl.NextBreakText}";
            }
        };
        desktop.Exit += (_, _) => tray.Dispose();
    }
}
