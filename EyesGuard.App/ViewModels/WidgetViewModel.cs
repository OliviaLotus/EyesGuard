using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 桌面小部件视图模型 - 负责桌面悬浮窗的状态显示
/// </summary>
public partial class WidgetViewModel : ViewModelBase
{
    private readonly BreakControlViewModel _breakControl;

    public WidgetViewModel(BreakControlViewModel breakControl)
    {
        _breakControl = breakControl;
        _breakControl.PropertyChanged += OnBreakControlChanged;
        UpdateWidgetState();
    }

    [ObservableProperty] private string _widgetStatusText = "准备中";
    [ObservableProperty] private string _widgetMessage = "正在启动护眼服务";
    [ObservableProperty] private string _widgetAccentColor = "#2DB879";

    private void OnBreakControlChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BreakControlViewModel.PhaseText)
            or nameof(BreakControlViewModel.RestRemainingText)
            or nameof(BreakControlViewModel.NextBreakText)
            or nameof(BreakControlViewModel.WorkProgress)
            or nameof(BreakControlViewModel.IsResting)
            or nameof(BreakControlViewModel.IsBreakPrompt))
        {
            UpdateWidgetState();
        }
    }

    private void UpdateWidgetState()
    {
        WidgetStatusText = _breakControl.PhaseText;

        if (_breakControl.IsResting)
        {
            WidgetMessage = $"请远眺并放松眼睛，还剩 {_breakControl.RestRemainingText} 秒";
            WidgetAccentColor = "#E85A68";
        }
        else if (_breakControl.IsBreakPrompt)
        {
            WidgetMessage = "现在该休息了，给眼睛一点时间";
            WidgetAccentColor = "#E85A68";
        }
        else if (_breakControl.WorkProgress >= 80)
        {
            WidgetMessage = $"下次休息还有 {_breakControl.NextBreakText}";
            WidgetAccentColor = "#F29A38";
        }
        else
        {
            WidgetMessage = $"下次休息还有 {_breakControl.NextBreakText}";
            WidgetAccentColor = "#2DB879";
        }
    }

    public void Dispose()
    {
        _breakControl.PropertyChanged -= OnBreakControlChanged;
    }
}
