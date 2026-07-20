using System.Runtime.InteropServices;
using EyesGuard.Core.Abstractions;

namespace EyesGuard.Platform.Windows;

public sealed class WindowsIdleTimeProvider : IIdleTimeProvider
{
    public TimeSpan GetIdleTime()
    {
        if (!OperatingSystem.IsWindows())
        {
            return TimeSpan.Zero;
        }

        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var elapsed = unchecked((uint)Environment.TickCount - info.Time);
        return TimeSpan.FromMilliseconds(elapsed);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo inputInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }
}
