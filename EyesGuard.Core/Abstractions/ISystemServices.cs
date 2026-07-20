namespace EyesGuard.Core.Abstractions;

/// <summary>
/// 系统服务接口 - 整合常用的系统级功能
/// </summary>
public interface ISystemServices
{
    /// <summary>
    /// 获取当前时间
    /// </summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// 获取系统空闲时间
    /// </summary>
    TimeSpan GetIdleTime();

    /// <summary>
    /// 检查是否有任何指定的进程正在运行
    /// </summary>
    bool IsAnyProcessRunning(IEnumerable<string> processNames);
}
