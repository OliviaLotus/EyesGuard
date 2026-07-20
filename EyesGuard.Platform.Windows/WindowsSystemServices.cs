using EyesGuard.Core.Abstractions;

namespace EyesGuard.Platform.Windows;

/// <summary>
/// Windows 系统服务实现 - 整合时钟、空闲时间和进程检测
/// </summary>
public class WindowsSystemServices : ISystemServices
{
    private readonly WindowsIdleTimeProvider _idleTimeProvider = new();
    private readonly WindowsProcessDetector _processDetector = new();

    public DateTimeOffset Now => DateTimeOffset.Now;

    public TimeSpan GetIdleTime() => _idleTimeProvider.GetIdleTime();

    public bool IsAnyProcessRunning(IEnumerable<string> processNames) =>
        _processDetector.IsAnyRunning(processNames);
}
