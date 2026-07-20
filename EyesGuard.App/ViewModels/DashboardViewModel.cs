using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyesGuard.Application.Services;

namespace EyesGuard.App.ViewModels;

/// <summary>
/// 仪表板视图模型 - 负责使用统计和健康报告的显示
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly UsageReportingService _reportingService;

    public DashboardViewModel(UsageReportingService reportingService)
    {
        _reportingService = reportingService;
        _reportingService.SummaryChanged += OnSummaryChanged;
    }

    [ObservableProperty] private string _todayUsageText = "0分钟";
    [ObservableProperty] private string _longestSessionText = "0分钟";
    [ObservableProperty] private string _nightUsageText = "0分钟";
    [ObservableProperty] private int _breakCount;
    [ObservableProperty] private int _completionRate = 100;
    [ObservableProperty] private int _healthScore = 100;
    [ObservableProperty] private string _weeklyAverageUsageText = "0分钟";
    [ObservableProperty] private string _weeklyTrendText = "暂无趋势数据";
    [ObservableProperty] private string _goalComparisonText = "今日目标：6小时以内";
    [ObservableProperty] private string _weeklyAdviceText = "保持规律休息，连续用眼达到计划时优先完成休息。";
    [ObservableProperty] private string _monthlyAverageUsageText = "0分钟";
    [ObservableProperty] private string _reportUpdatedText = "报告尚未刷新";

    public ObservableCollection<DailyBarItem> RecentDays { get; } = [];
    public ObservableCollection<DailyBarItem> MonthlyDays { get; } = [];

    public async Task InitializeAsync()
    {
        await RefreshSummaryAsync();
    }

    [RelayCommand]
    private async Task RefreshReportAsync()
    {
        await RefreshSummaryAsync();
    }

    private void OnSummaryChanged(object? sender, EventArgs args)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () => await RefreshSummaryAsync());
    }

    private async Task RefreshSummaryAsync()
    {
        var today = await _reportingService.GetTodaySummaryAsync();
        TodayUsageText = UsageReportingService.FormatDuration(today.ActiveTime);
        LongestSessionText = UsageReportingService.FormatDuration(today.LongestSession);
        NightUsageText = UsageReportingService.FormatDuration(today.NightTime);
        BreakCount = today.BreaksCompleted;
        CompletionRate = today.CompletionRate;
        HealthScore = _reportingService.CalculateHealthScore(today);

        var days = await _reportingService.GetRecentSummariesAsync(7);
        var monthDays = await _reportingService.GetRecentSummariesAsync(30);
        var previousWeek = await _reportingService.GetRecentSummariesAsync(14);
        var previousWeekDays = previousWeek.Take(7).ToArray();

        UpdateRecentDaysChart(days);
        UpdateMonthlyDaysChart(monthDays);
        UpdateWeeklyStatistics(days, previousWeekDays, today);
        UpdateMonthlyStatistics(monthDays);

        ReportUpdatedText = $"更新于 {DateTime.Now:MM月dd日 HH:mm}";
    }

    private void UpdateRecentDaysChart(IReadOnlyList<Core.Models.UsageSummary> days)
    {
        var maxMinutes = Math.Max(1, days.Max(x => x.ActiveTime.TotalMinutes));
        RecentDays.Clear();
        foreach (var day in days)
        {
            RecentDays.Add(new DailyBarItem(
                day.Day.ToString("MM/dd"),
                Math.Max(4, day.ActiveTime.TotalMinutes / maxMinutes * 120),
                UsageReportingService.FormatDuration(day.ActiveTime)));
        }
    }

    private void UpdateMonthlyDaysChart(IReadOnlyList<Core.Models.UsageSummary> monthDays)
    {
        var maxMonthMinutes = Math.Max(1, monthDays.Max(x => x.ActiveTime.TotalMinutes));
        MonthlyDays.Clear();
        for (var index = 0; index < monthDays.Count; index++)
        {
            var day = monthDays[index];
            MonthlyDays.Add(new DailyBarItem(
                day.Day.ToString("MM/dd"),
                Math.Max(4, day.ActiveTime.TotalMinutes / maxMonthMinutes * 120),
                UsageReportingService.FormatDuration(day.ActiveTime),
                index == 0 || index == monthDays.Count - 1 || index % 5 == 4));
        }
    }

    private void UpdateWeeklyStatistics(
        IReadOnlyList<Core.Models.UsageSummary> days,
        Core.Models.UsageSummary[] previousWeekDays,
        Core.Models.UsageSummary today)
    {
        var weeklyAverage = TimeSpan.FromTicks(days.Sum(x => x.ActiveTime.Ticks) / Math.Max(1, days.Count));
        WeeklyAverageUsageText = UsageReportingService.FormatDuration(weeklyAverage);

        var currentTotal = days.Sum(x => x.ActiveTime.TotalMinutes);
        var previousTotal = previousWeekDays.Sum(x => x.ActiveTime.TotalMinutes);
        WeeklyTrendText = _reportingService.FormatTrend(currentTotal, previousTotal);

        GoalComparisonText = today.ActiveTime <= TimeSpan.FromHours(6)
            ? $"今日目标：6小时以内，还剩 {UsageReportingService.FormatDuration(TimeSpan.FromHours(6) - today.ActiveTime)}"
            : $"今日已超过建议值 {UsageReportingService.FormatDuration(today.ActiveTime - TimeSpan.FromHours(6))}";

        WeeklyAdviceText = _reportingService.BuildWeeklyAdvice(days, today);
    }

    private void UpdateMonthlyStatistics(IReadOnlyList<Core.Models.UsageSummary> monthDays)
    {
        MonthlyAverageUsageText = UsageReportingService.FormatDuration(
            TimeSpan.FromTicks(monthDays.Sum(x => x.ActiveTime.Ticks) / Math.Max(1, monthDays.Count)));
    }

    public void Dispose()
    {
        _reportingService.SummaryChanged -= OnSummaryChanged;
    }
}
