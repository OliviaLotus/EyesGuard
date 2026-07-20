using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 更新管理视图模型 - 负责软件更新检查和安装
/// </summary>
public partial class UpdateViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;
    private UpdateCheckResult? _availableUpdate;

    public UpdateViewModel(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    [ObservableProperty] private string _updateStatusText = "尚未检查更新";
    [ObservableProperty] private bool _updateAvailable;

    [RelayCommand]
    private async Task CheckForUpdatesAsync(string manifestUrl)
    {
        var result = await _updateService.CheckAsync(manifestUrl);
        _availableUpdate = result.Status == UpdateCheckStatus.Available ? result : null;
        UpdateAvailable = _availableUpdate is not null;
        UpdateStatusText = result.Message;
        if (result.Status == UpdateCheckStatus.Available)
        {
            UpdateStatusText = $"发现新版本 {result.AvailableVersion}：{result.Message}";
        }
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        if (_availableUpdate is null)
        {
            return;
        }

        var result = await _updateService.DownloadAndLaunchAsync(_availableUpdate);
        UpdateStatusText = result.Message;
        if (result.Status != UpdateCheckStatus.Available)
        {
            _availableUpdate = null;
            UpdateAvailable = false;
        }
    }
}
