using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;
using EyesGuard.Core.Services;

namespace EyesGuard.Application.Services;

/// <summary>
/// 使用报告服务 - 处理使用统计和报告生成
/// </summary>
public class UsageReportingService
{
    private readonly BreakReminderService _breakService;

    public UsageReportingService(BreakReminderService breakService)
    {
        _breakService = breakService;
    }

    public event EventHandler? SummaryChanged
    {
        add => _breakService.SummaryChanged += value;
        remove => _breakService.SummaryChanged -= value;
    }

    public Task<UsageSummary> GetTodaySummaryAsync(CancellationToken cancellationToken = default)
        => _breakService.GetTodaySummaryAsync(cancellationToken);

    public Task<IReadOnlyList<UsageSummary>> GetRecentSummariesAsync(int days, CancellationToken cancellationToken = default)
        => _breakService.GetRecentSummariesAsync(days, cancellationToken);

    /// <summary>
    /// 计算健康评分
    /// </summary>
    public int CalculateHealthScore(UsageSummary summary)
    {
        var usagePenalty = summary.ActiveTime > TimeSpan.FromHours(6)
            ? Math.Min(25, (int)Math.Ceiling((summary.ActiveTime - TimeSpan.FromHours(6)).TotalMinutes / 15) * 2)
            : 0;
        var nightPenalty = Math.Min(20, (int)Math.Ceiling(summary.NightTime.TotalMinutes / 15) * 2);
        var restPenalty = Math.Min(35, summary.BreaksSkipped * 8 + summary.BreaksSnoozed * 2);
        return Math.Clamp(100 - usagePenalty - nightPenalty - restPenalty, 0, 100);
    }

    /// <summary>
    /// 生成每周建议
    /// </summary>
    public string BuildWeeklyAdvice(IReadOnlyList<UsageSummary> days, UsageSummary today)
    {
        if (days.Any(x => x.CompletionRate < 70))
        {
            return "本周有休息完成率偏低的日期，建议减少推迟和跳过，优先完成短暂休息。";
        }

        if (days.Sum(x => x.NightTime.Ticks) > TimeSpan.FromHours(3).Ticks)
        {
            return "本周夜间用眼偏多，建议在睡前开启暖色模式并提前结束屏幕使用。";
        }

        if (today.ActiveTime > TimeSpan.FromHours(6))
        {
            return "今日用眼已超过建议值，安排一次完整休息后再继续使用屏幕。";
        }

        return "本周节奏保持良好，继续按计划休息并保持屏幕距离。";
    }

    /// <summary>
    /// 格式化趋势文本
    /// </summary>
    public string FormatTrend(double currentMinutes, double previousMinutes)
    {
        if (previousMinutes <= 0)
        {
            return "与上周相比暂无足够数据";
        }

        var change = (currentMinutes - previousMinutes) / previousMinutes * 100;
        return change switch
        {
            > 0.5 => $"比上周增加 {change:0}%",
            < -0.5 => $"比上周减少 {Math.Abs(change):0}%",
            _ => "与上周基本持平"
        };
    }

    /// <summary>
    /// 格式化时长
    /// </summary>
    public static string FormatDuration(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}小时{value.Minutes}分钟"
        : $"{Math.Max(0, (int)value.TotalMinutes)}分钟";
}
