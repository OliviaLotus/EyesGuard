using System.Diagnostics;
using EyesGuard.Core.Abstractions;

namespace EyesGuard.Platform.Windows;

public sealed class WindowsProcessDetector : IProcessDetector
{
    public bool IsAnyRunning(IEnumerable<string> processNames)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        foreach (var name in processNames)
        {
            var normalized = Path.GetFileNameWithoutExtension(name.Trim());
            if (normalized.Length == 0)
            {
                continue;
            }

            try
            {
                if (Process.GetProcessesByName(normalized).Length > 0)
                {
                    return true;
                }
            }
            catch (SystemException)
            {
                // A process can exit while it is being enumerated.
            }
        }

        return false;
    }
}
