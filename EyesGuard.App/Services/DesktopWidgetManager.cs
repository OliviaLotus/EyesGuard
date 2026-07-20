using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using EyesGuard.App.ViewModels;
using EyesGuard.App.Views;

namespace EyesGuard.App.Services;

public sealed class DesktopWidgetManager : IDisposable
{
    private readonly MainWindow _host;
    private readonly MainWindowViewModel _viewModel;
    private DesktopWidgetWindow? _window;
    private bool _hostOpened;
    private bool _positioned;

    public DesktopWidgetManager(MainWindow host, MainWindowViewModel viewModel)
    {
        _host = host;
        _viewModel = viewModel;
        _host.Opened += OnHostOpened;
        _host.Screens.Changed += OnScreensChanged;
        _viewModel.Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnHostOpened(object? sender, EventArgs args)
    {
        _hostOpened = true;
        UpdateVisibility();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SettingsViewModel.DesktopWidgetEnabled))
        {
            UpdateVisibility();
        }
    }

    private void OnScreensChanged(object? sender, EventArgs args)
    {
        if (_window is not null && _viewModel.Settings.DesktopWidgetEnabled)
        {
            _positioned = false;
            PositionNearWorkingArea(_window);
            _positioned = true;
        }
    }

    private void UpdateVisibility()
    {
        if (!_hostOpened)
        {
            return;
        }

        if (!_viewModel.Settings.DesktopWidgetEnabled)
        {
            _window?.Hide();
            return;
        }

        _window ??= new DesktopWidgetWindow { DataContext = _viewModel };
        if (!_positioned)
        {
            PositionNearWorkingArea(_window);
            _positioned = true;
        }

        _window.Show();
    }

    private void PositionNearWorkingArea(Window window)
    {
        var screen = _host.Screens.ScreenFromWindow(_host) ?? _host.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        const int margin = 18;
        var scale = screen.Scaling <= 0 ? 1 : screen.Scaling;
        var area = screen.WorkingArea;
        var width = (int)Math.Ceiling(window.Width * scale);
        var height = (int)Math.Ceiling(window.Height * scale);
        window.Position = new PixelPoint(
            area.X + Math.Max(0, area.Width - width - margin),
            area.Y + Math.Max(0, area.Height - height - margin));
    }

    public void Dispose()
    {
        _host.Opened -= OnHostOpened;
        _host.Screens.Changed -= OnScreensChanged;
        _viewModel.Settings.PropertyChanged -= OnSettingsPropertyChanged;
        _window?.CloseForApplicationExit();
        _window = null;
    }
}
