using System.Runtime.InteropServices;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.Platform.Windows;

/// <summary>Controls external displays through the standard DDC/CI brightness VCP feature.</summary>
public sealed class WindowsDisplayControlService : IDisplayControlService
{
    private const byte BrightnessVcpCode = 0x10;

    public DisplayControlCapabilities GetCapabilities()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new(false, false, false, 0, "当前平台不支持 Windows 显示器控制");
        }

        var monitors = GetPhysicalMonitors();
        if (monitors.Count == 0)
        {
            return new(false, false, false, 0, "未检测到支持 DDC/CI 的显示器");
        }

        try
        {
            var readable = monitors.Any(TryReadBrightness);
            return new(
                readable,
                readable,
                readable,
                monitors.Count,
                readable
                    ? $"已检测到 {monitors.Count} 台显示器，可通过 DDC/CI 调节亮度"
                    : "显示器未提供 DDC/CI 亮度控制");
        }
        finally
        {
            DestroyPhysicalMonitors(monitors);
        }
    }

    public bool TryGetBrightness(out int brightnessPercent)
    {
        brightnessPercent = 0;
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var monitors = GetPhysicalMonitors();
        try
        {
            foreach (var monitor in monitors)
            {
                if (TryReadBrightness(monitor, out brightnessPercent))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            DestroyPhysicalMonitors(monitors);
        }
    }

    public bool TrySetBrightness(int brightnessPercent, out string status)
    {
        brightnessPercent = Math.Clamp(brightnessPercent, 0, 100);
        if (!OperatingSystem.IsWindows())
        {
            status = "当前平台不支持 Windows 显示器控制";
            return false;
        }

        var monitors = GetPhysicalMonitors();
        if (monitors.Count == 0)
        {
            status = "未检测到支持 DDC/CI 的显示器";
            return false;
        }

        var changed = 0;
        try
        {
            foreach (var monitor in monitors)
            {
                if (SetVCPFeature(monitor.Handle, BrightnessVcpCode, (uint)brightnessPercent))
                {
                    changed++;
                }
            }
        }
        finally
        {
            DestroyPhysicalMonitors(monitors);
        }

        status = changed == 0
            ? "显示器不支持 DDC/CI 亮度调节"
            : $"已将 {changed} 台显示器亮度设为 {brightnessPercent}%";
        return changed > 0;
    }

    private static List<PhysicalMonitor> GetPhysicalMonitors()
    {
        var result = new List<PhysicalMonitor>();
        if (!OperatingSystem.IsWindows())
        {
            return result;
        }

        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
            {
                try
                {
                    if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out var count) || count == 0)
                    {
                        return true;
                    }

                    var monitors = new PhysicalMonitor[count];
                    if (GetPhysicalMonitorsFromHMONITOR(monitor, count, monitors))
                    {
                        result.AddRange(monitors);
                    }
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }

                return true;
            }, IntPtr.Zero);
        }
        catch (DllNotFoundException)
        {
            result.Clear();
        }
        catch (EntryPointNotFoundException)
        {
            result.Clear();
        }
        return result;
    }

    private static bool TryReadBrightness(PhysicalMonitor monitor) => TryReadBrightness(monitor, out _);

    private static bool TryReadBrightness(PhysicalMonitor monitor, out int brightnessPercent)
    {
        brightnessPercent = 0;
        if (!GetVCPFeatureAndVCPFeatureReply(monitor.Handle, BrightnessVcpCode, out _, out var current, out var maximum))
        {
            return false;
        }

        if (maximum == 0)
        {
            return false;
        }

        brightnessPercent = (int)Math.Clamp(Math.Round(current * 100d / maximum), 0, 100);
        return true;
    }

    private static void DestroyPhysicalMonitors(List<PhysicalMonitor> monitors)
    {
        foreach (var monitor in monitors)
        {
            try { DestroyPhysicalMonitor(monitor.Handle); }
            catch (DllNotFoundException) { }
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint count);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint count, [Out] PhysicalMonitor[] monitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr physicalMonitor, byte code,
        out byte type, out uint currentValue, out uint maximumValue);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetVCPFeature(IntPtr physicalMonitor, byte code, uint value);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyPhysicalMonitor(IntPtr physicalMonitor);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PhysicalMonitor
    {
        public IntPtr Handle;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
    }
}
