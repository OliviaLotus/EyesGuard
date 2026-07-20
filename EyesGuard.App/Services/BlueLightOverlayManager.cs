using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EyesGuard.Core.Models;
using EyesGuard.App.Views;

namespace EyesGuard.App.Services;

public sealed class BlueLightOverlayManager : IDisposable
{
    private readonly Window _host;
    private readonly List<BlueLightOverlayWindow> _overlays = [];
    private bool _enabled;
    private int _intensity;
    private ColorVisionMode _colorVisionMode;
    private bool _disposed;

    public BlueLightOverlayManager(Window host)
    {
        _host = host;
        _host.Screens.Changed += OnScreensChanged;
        _host.Opened += OnHostOpened;
    }

    public void Update(bool enabled, int intensity)
        => Update(enabled, intensity, ColorVisionMode.None);

    public void Update(bool enabled, int intensity, ColorVisionMode colorVisionMode)
    {
        if (_disposed)
        {
            return;
        }

        _enabled = enabled;
        _intensity = Math.Clamp(intensity, 0, 100);
        _colorVisionMode = colorVisionMode;
        if ((!_enabled || _intensity == 0) && _colorVisionMode == ColorVisionMode.None)
        {
            HideAll();
            return;
        }

        RefreshWindows();
    }

    private void OnScreensChanged(object? sender, EventArgs args)
    {
        if ((_enabled && _intensity > 0) || _colorVisionMode != ColorVisionMode.None)
        {
            RefreshWindows();
        }
    }

    private void OnHostOpened(object? sender, EventArgs args)
    {
        if ((_enabled && _intensity > 0) || _colorVisionMode != ColorVisionMode.None)
        {
            RefreshWindows();
        }
    }

    private void RefreshWindows()
    {
        var screens = _host.Screens.All;
        if (screens.Count == 0)
        {
            HideAll();
            return;
        }

        while (_overlays.Count > screens.Count)
        {
            var last = _overlays[^1];
            _overlays.RemoveAt(_overlays.Count - 1);
            last.Close();
        }

        while (_overlays.Count < screens.Count)
        {
            var overlay = CreateOverlay();
            _overlays.Add(overlay);
        }

        var brush = CreateBrush(_enabled, _intensity, _colorVisionMode);
        for (var index = 0; index < screens.Count; index++)
        {
            var screen = screens[index];
            var overlay = _overlays[index];
            var scale = screen.Scaling <= 0 ? 1 : screen.Scaling;
            overlay.Position = screen.Bounds.Position;
            overlay.Width = screen.Bounds.Width / scale;
            overlay.Height = screen.Bounds.Height / scale;
            overlay.Background = brush;
            overlay.Show();
            SetClickThrough(overlay);
        }
    }

    private BlueLightOverlayWindow CreateOverlay()
    {
        var overlay = new BlueLightOverlayWindow
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
            TransparencyBackgroundFallback = Brushes.Transparent,
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
            IsHitTestVisible = false
        };
        overlay.Opened += (_, _) => SetClickThrough(overlay);
        return overlay;
    }

    private static IBrush CreateBrush(bool blueLightEnabled, int intensity, ColorVisionMode colorVisionMode)
    {
        // The color-vision layer is intentionally subtle so text remains readable.
        var blueAlpha = blueLightEnabled ? Math.Clamp(Math.Round(intensity * 1.07), 0, 107) : 0;
        var vision = colorVisionMode switch
        {
            ColorVisionMode.Protanopia => (Red: 86d, Green: 120d, Blue: 255d, Alpha: 46d),
            ColorVisionMode.Deuteranopia => (Red: 205d, Green: 86d, Blue: 220d, Alpha: 46d),
            ColorVisionMode.Tritanopia => (Red: 255d, Green: 174d, Blue: 76d, Alpha: 46d),
            _ => (Red: 255d, Green: 190d, Blue: 110d, Alpha: 0d)
        };

        if (vision.Alpha <= 0)
        {
            return new SolidColorBrush(Color.FromArgb((byte)blueAlpha, 255, 190, 110));
        }

        var totalAlpha = Math.Max(vision.Alpha, blueAlpha);
        var weight = vision.Alpha / Math.Max(1d, totalAlpha);
        var red = vision.Red * weight + 255d * (1d - weight);
        var green = vision.Green * weight + 190d * (1d - weight);
        var blue = vision.Blue * weight + 110d * (1d - weight);
        return new SolidColorBrush(Color.FromArgb(
            (byte)Math.Clamp(Math.Round(totalAlpha), 0, 255),
            (byte)Math.Clamp(Math.Round(red), 0, 255),
            (byte)Math.Clamp(Math.Round(green), 0, 255),
            (byte)Math.Clamp(Math.Round(blue), 0, 255)));
    }

    private void HideAll()
    {
        foreach (var overlay in _overlays)
        {
            overlay.Hide();
        }
    }

    private static void SetClickThrough(Window window)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        style |= WsExTransparent | WsExLayered | WsExToolWindow | WsExNoActivate;
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style));
        SetWindowPos(handle, HwndTopMost, 0, 0, 0, 0,
            SwpNoActivate | SwpNoMove | SwpNoSize | SwpShowWindow);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _host.Screens.Changed -= OnScreensChanged;
        _host.Opened -= OnHostOpened;
        foreach (var overlay in _overlays)
        {
            overlay.Close();
        }
        _overlays.Clear();
    }

    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20;
    private const long WsExLayered = 0x80000;
    private const long WsExToolWindow = 0x80;
    private const long WsExNoActivate = 0x08000000;
    private static readonly IntPtr HwndTopMost = new(-1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpNoActivate = 0x0010;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
