using CommunityToolkit.Mvvm.ComponentModel;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 亮度控制视图模型 - 负责显示器亮度管理
/// </summary>
public partial class BrightnessViewModel : ViewModelBase
{
    private readonly IDisplayControlService _displayControlService;
    private readonly DisplayControlCapabilities _displayCapabilities;
    private bool _isApplyingBrightness;

    public BrightnessViewModel(IDisplayControlService displayControlService)
    {
        _displayControlService = displayControlService;
        _displayCapabilities = displayControlService.GetCapabilities();
        BrightnessSupported = _displayCapabilities.CanWriteBrightness;
        BrightnessStatus = _displayCapabilities.Status;

        if (_displayCapabilities.CanReadBrightness && displayControlService.TryGetBrightness(out var brightness))
        {
            BrightnessPercent = brightness;
        }
    }

    [ObservableProperty] private bool _brightnessSupported;
    [ObservableProperty] private int _brightnessPercent = 50;
    [ObservableProperty] private string _brightnessStatus = "正在检测显示器亮度能力";

    partial void OnBrightnessPercentChanged(int value)
    {
        if (_isApplyingBrightness || !BrightnessSupported)
        {
            return;
        }

        _displayControlService.TrySetBrightness(value, out var status);
        BrightnessStatus = status;
    }

    public void ApplyBrightness(int brightnessPercent)
    {
        brightnessPercent = Math.Clamp(brightnessPercent, 0, 100);
        _isApplyingBrightness = true;
        try
        {
            BrightnessPercent = brightnessPercent;
        }
        finally
        {
            _isApplyingBrightness = false;
        }

        if (BrightnessSupported)
        {
            _displayControlService.TrySetBrightness(brightnessPercent, out var status);
            BrightnessStatus = status;
        }
    }

    public int? CaptureCurrentBrightness()
    {
        if (!BrightnessSupported)
        {
            return null;
        }

        return _displayControlService.TryGetBrightness(out var brightness)
            ? Math.Clamp(brightness, 0, 100)
            : Math.Clamp(BrightnessPercent, 0, 100);
    }
}
