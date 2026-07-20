using CommunityToolkit.Mvvm.ComponentModel;
using EyesGuard.Core.Abstractions;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 启动设置视图模型 - 负责开机启动设置
/// </summary>
public partial class StartupViewModel : ViewModelBase
{
    private readonly IStartupService _startupService;
    private bool _isApplyingSettings;

    public StartupViewModel(IStartupService startupService)
    {
        _startupService = startupService;
        StartWithWindows = startupService.IsEnabled;
    }

    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private string _statusMessage = string.Empty;

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_isApplyingSettings || !_startupService.IsSupported)
        {
            return;
        }

        if (!_startupService.TrySetEnabled(value, out var status))
        {
            _isApplyingSettings = true;
            StartWithWindows = _startupService.IsEnabled;
            _isApplyingSettings = false;
        }

        StatusMessage = status;
    }
}
