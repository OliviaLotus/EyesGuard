namespace EyesGuard.Application.Services;

/// <summary>
/// 蓝光调度服务 - 处理蓝光过滤的自动开关逻辑
/// </summary>
public class BlueLightSchedulingService
{
    private bool? _manualOverride;

    /// <summary>
    /// 手动覆盖状态
    /// </summary>
    public bool? ManualOverride
    {
        get => _manualOverride;
        set => _manualOverride = value;
    }

    /// <summary>
    /// 清除手动覆盖
    /// </summary>
    public void ClearManualOverride()
    {
        _manualOverride = null;
    }

    /// <summary>
    /// 计算当前是否应该启用蓝光过滤
    /// </summary>
    public bool ShouldEnableBlueLightFilter(
        bool scheduleEnabled,
        TimeSpan currentTime,
        TimeSpan scheduleStart,
        TimeSpan scheduleEnd)
    {
        if (!scheduleEnabled || _manualOverride.HasValue)
        {
            return false; // 调度未启用或存在手动覆盖
        }

        return IsInTimeRange(currentTime, scheduleStart, scheduleEnd);
    }

    /// <summary>
    /// 判断时间是否在指定范围内（支持跨午夜）
    /// </summary>
    public static bool IsInTimeRange(TimeSpan current, TimeSpan start, TimeSpan end)
    {
        if (start <= end)
        {
            // 不跨午夜：start=20:00, end=23:00
            return current >= start && current < end;
        }
        else
        {
            // 跨午夜：start=22:00, end=07:00
            return current >= start || current < end;
        }
    }
}
