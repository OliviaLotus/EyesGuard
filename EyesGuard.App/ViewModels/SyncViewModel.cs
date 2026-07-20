using CommunityToolkit.Mvvm.ComponentModel;
using EyesGuard.Core.Services;
using EyesGuard.Core.Models;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 同步状态视图模型 - 负责同步状态的显示
/// </summary>
public partial class SyncViewModel : ViewModelBase
{
    private readonly SyncService _syncService;

    public SyncViewModel(SyncService syncService)
    {
        _syncService = syncService;
    }

    [ObservableProperty] private string _syncStatusText = "同步尚未配置服务器";
    [ObservableProperty] private int _pendingSyncCount;

    public async Task RefreshSyncStatusAsync()
    {
        var syncResult = await _syncService.TrySyncAsync();
        ApplySyncResult(syncResult);
    }

    public void ApplySyncResult(SyncResult result)
    {
        PendingSyncCount = result.PendingCount;
        SyncStatusText = result.PendingCount > 0
            ? $"{result.Message}（{result.PendingCount} 条待同步）"
            : result.Message;
    }
}
