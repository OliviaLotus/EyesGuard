using Microsoft.Win32;
using System.Runtime.Versioning;
using EyesGuard.Core.Abstractions;

namespace EyesGuard.Platform.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsStartupService : IStartupService
{
    private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "EyesGuard";

    public bool IsSupported => OperatingSystem.IsWindows();
    public bool IsEnabled => IsSupported && ReadValue() is not null;

    public bool TrySetEnabled(bool enabled, out string status)
    {
        if (!IsSupported)
        {
            status = "当前平台不支持 Windows 开机启动";
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (key is null)
            {
                status = "无法访问当前用户开机启动项";
                return false;
            }

            if (enabled)
            {
                var executable = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executable))
                {
                    status = "无法确定应用程序路径";
                    return false;
                }

                key.SetValue(ValueName, $"\"{executable}\"");
                status = "已开启 Windows 开机启动";
            }
            else
            {
                key.DeleteValue(ValueName, false);
                status = "已关闭 Windows 开机启动";
            }

            return true;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            status = $"设置开机启动失败：{exception.Message}";
            return false;
        }
    }

    private static string? ReadValue()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(ValueName) as string;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }
}
